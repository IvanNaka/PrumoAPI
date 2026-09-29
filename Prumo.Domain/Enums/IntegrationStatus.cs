namespace Prumo.Domain.Enums
{
    // Figura 29.
    public enum IntegrationStatus
    {
        NaoConfigurada,
        Configurada,
        TestandoConexao,
        Conectada,
        ErroConexao,
        Sincronizando,
        FalhaSincronizacao
    }
}
