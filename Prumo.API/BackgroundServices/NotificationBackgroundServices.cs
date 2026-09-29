using System.Threading.Channels;
using Prumo.Application.Interfaces;

namespace Prumo.API.BackgroundServices
{
    /// <summary>Fila do dispatcher de notificações (Channel&lt;Guid&gt;).</summary>
    public class NotificationQueue : INotificationQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

        public ChannelReader<Guid> Reader => _channel.Reader;

        public void Enqueue(Guid notificationId) => _channel.Writer.TryWrite(notificationId);
    }

    /// <summary>
    /// NotificacaoDispatcher (Figura 30): lê a fila e marca cada notificação como Enviada (DataEnvio).
    /// Na subida, reenfileira as que ficaram em Gerada/Enfileirada.
    /// </summary>
    public class NotificationDispatcher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly NotificationQueue _queue;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(IServiceScopeFactory scopeFactory, NotificationQueue queue, ILogger<NotificationDispatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                    foreach (var id in db.Alerts
                                 .Where(a => a.Status == Domain.Enums.AlertStatus.Gerada || a.Status == Domain.Enums.AlertStatus.Enfileirada)
                                 .Select(a => a.Id)
                                 .ToList())
                    {
                        _queue.Enqueue(id);
                    }
                }

                await foreach (var id in _queue.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        await scope.ServiceProvider.GetRequiredService<INotificationService>().DispatchAsync(id, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Falha ao despachar a notificação {Id}.", id);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no dispatcher de notificações.");
            }
        }
    }

    /// <summary>
    /// Job diário das notificações (07:00, horário do servidor): regras (RegrasNotificacaoService) e
    /// expiração (Enviada 7 dias → Ignorada; Lida/Ignorada 30 dias → Arquivada).
    /// </summary>
    public class DailyNotificationJob : BackgroundService
    {
        public static readonly TimeOnly Horario = new(7, 0);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyNotificationJob> _logger;

        public DailyNotificationJob(IServiceScopeFactory scopeFactory, ILogger<DailyNotificationJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>Próxima ocorrência das 07:00 depois de <paramref name="agora"/> (hora local).</summary>
        public static DateTime ProximaExecucao(DateTime agora)
        {
            var hoje = agora.Date.Add(Horario.ToTimeSpan());
            return agora < hoje ? hoje : hoje.AddDays(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var agora = DateTime.Now;
                    await Task.Delay(ProximaExecucao(agora) - agora, stoppingToken);

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var criadas = await scope.ServiceProvider.GetRequiredService<INotificationRulesService>().ExecuteAsync(stoppingToken);
                        await scope.ServiceProvider.GetRequiredService<INotificationService>().ExpireAsync(DateTime.UtcNow, stoppingToken);
                        _logger.LogInformation("Job diário de notificações: {Criadas} notificação(ões) gerada(s).", criadas);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Falha no job diário de notificações.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }
}
