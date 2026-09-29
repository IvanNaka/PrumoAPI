namespace Prumo.Domain.Enums
{
    // Ciclo de vida da notificação (Figura 30, D12).
    public enum AlertStatus
    {
        Gerada,
        Enfileirada,
        Enviada,
        Lida,
        Ignorada,
        Arquivada
    }
}
