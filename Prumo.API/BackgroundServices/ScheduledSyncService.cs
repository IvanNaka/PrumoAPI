using System.Threading.Channels;
using Prumo.Application.Interfaces;

namespace Prumo.API.BackgroundServices
{
    /// <summary>Fila da sincronização manual ("Sincronizar agora").</summary>
    public class SyncTrigger : ISyncTrigger
    {
        private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
        });

        public ChannelReader<bool> Reader => _channel.Reader;

        public void Trigger() => _channel.Writer.TryWrite(true);
    }

    /// <summary>
    /// SincronizacaoAgendadaService (RF51): a cada minuto verifica se chegou a hora de sincronizar e
    /// também atende a fila da sincronização manual. Nunca roda duas sincronizações ao mesmo tempo.
    /// </summary>
    public class ScheduledSyncService : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SyncTrigger _trigger;
        private readonly ILogger<ScheduledSyncService> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public ScheduledSyncService(IServiceScopeFactory scopeFactory, SyncTrigger trigger, ILogger<ScheduledSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _trigger = trigger;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await WithServiceAsync(s => s.RecoverInterruptedSyncAsync(), stoppingToken);
            await Task.WhenAll(ManualLoopAsync(stoppingToken), ScheduledLoopAsync(stoppingToken));
        }

        private async Task ManualLoopAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (await _trigger.Reader.WaitToReadAsync(stoppingToken))
                {
                    _trigger.Reader.TryRead(out _);
                    await _lock.WaitAsync(stoppingToken);
                    try
                    {
                        await WithServiceAsync(s => s.RunJiraSyncAsync(stoppingToken), stoppingToken);
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task ScheduledLoopAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Tick);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    // Se já houver uma sincronização rodando, pula este minuto.
                    if (!await _lock.WaitAsync(0, stoppingToken))
                    {
                        continue;
                    }

                    try
                    {
                        await WithServiceAsync(async s =>
                        {
                            if (await s.IsJiraSyncDueAsync(DateTime.UtcNow))
                            {
                                await s.RunJiraSyncAsync(stoppingToken);
                            }
                        }, stoppingToken);
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task WithServiceAsync(Func<IIntegrationSyncService, Task> action, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await action(scope.ServiceProvider.GetRequiredService<IIntegrationSyncService>());
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no serviço de sincronização agendada.");
            }
        }
    }
}
