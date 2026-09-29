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

        /// <summary>
        /// Deve rodar agora? Sim quando já passou das 07:00 (hora local) e o job ainda não rodou nesta data.
        /// Se a API subir depois das 07:00, o job do dia roda no primeiro minuto (a deduplicação evita repetição).
        /// </summary>
        public static bool DeveExecutar(DateTime agora, DateOnly? ultimaExecucao) =>
            TimeOnly.FromDateTime(agora) >= Horario && ultimaExecucao != DateOnly.FromDateTime(agora);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateOnly? ultimaExecucao = null;
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    var agora = DateTime.Now;
                    if (!DeveExecutar(agora, ultimaExecucao))
                    {
                        continue;
                    }

                    ultimaExecucao = DateOnly.FromDateTime(agora);
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
