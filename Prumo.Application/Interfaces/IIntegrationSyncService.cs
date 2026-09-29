using Prumo.Application.DTOs.Integration;

namespace Prumo.Application.Interfaces
{
    /// <summary>Sincronização de issues e worklogs (RF47, RF51, UC16).</summary>
    public interface IIntegrationSyncService
    {
        /// <summary>
        /// "Sincronizar agora": aplica Conectada/FalhaSincronizacao → Sincronizando e coloca a execução
        /// na fila do serviço em segundo plano (RN22 se o estado não permitir).
        /// </summary>
        Task<IntegracaoJiraDto> RequestJiraSyncAsync();

        /// <summary>RF51: a integração está ativa e chegou a hora da próxima sincronização?</summary>
        Task<bool> IsJiraSyncDueAsync(DateTime agora);

        /// <summary>Executa a sincronização (já com a exclusão mútua garantida por quem chama).</summary>
        Task RunJiraSyncAsync(CancellationToken cancellationToken);

        /// <summary>Na subida da API: sincronização interrompida (Sincronizando) volta para FalhaSincronizacao.</summary>
        Task RecoverInterruptedSyncAsync();
    }

    /// <summary>Fila da sincronização manual, consumida pelo serviço agendado.</summary>
    public interface ISyncTrigger
    {
        void Trigger();
    }

    /// <summary>Executado depois de cada sincronização bem-sucedida (ex.: regras de notificação — T21).</summary>
    public interface ISyncCompletedHandler
    {
        Task OnSyncCompletedAsync(CancellationToken cancellationToken);
    }

    /// <summary>Avisa os Administradores (RN26).</summary>
    public interface IAdminNotifier
    {
        Task NotifyAdminsAsync(string message, CancellationToken cancellationToken = default);
    }
}
