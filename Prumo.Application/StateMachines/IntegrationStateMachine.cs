using Prumo.Application.Common;
using Prumo.Domain.Enums;

namespace Prumo.Application.StateMachines
{
    /// <summary>Integração Jira (Figura 29, Seção 3.4.4).</summary>
    public static class IntegrationStateMachine
    {
        public const string Salvar = "SalvarCredenciais";
        public const string Testar = "TestarConexao";
        public const string Sucesso = "Sucesso";
        public const string Falha = "Falha";
        public const string Sincronizar = "Sincronizar";
        public const string SyncSucesso = "SincronizacaoConcluida";
        public const string SyncErro = "ErroSincronizacao";
        public const string TentarNovamente = "TentarNovamente";
        public const string TokenExpirado = "TokenExpirado";

        private static readonly Dictionary<(IntegrationStatus, string), IntegrationStatus> T = new()
        {
            {(IntegrationStatus.NaoConfigurada,     Salvar),          IntegrationStatus.Configurada},
            {(IntegrationStatus.ErroConexao,        Salvar),          IntegrationStatus.Configurada},
            // Editar a configuração de uma integração já conectada (ex.: mudar o intervalo) também
            // passa por Configurada e é testada de novo.
            {(IntegrationStatus.Configurada,        Salvar),          IntegrationStatus.Configurada},
            {(IntegrationStatus.Conectada,          Salvar),          IntegrationStatus.Configurada},
            {(IntegrationStatus.FalhaSincronizacao, Salvar),          IntegrationStatus.Configurada},

            {(IntegrationStatus.Configurada,        Testar),          IntegrationStatus.TestandoConexao},
            // "Testar conexão" manual a partir dos estados estáveis.
            {(IntegrationStatus.Conectada,          Testar),          IntegrationStatus.TestandoConexao},
            {(IntegrationStatus.ErroConexao,        Testar),          IntegrationStatus.TestandoConexao},
            {(IntegrationStatus.FalhaSincronizacao, Testar),          IntegrationStatus.TestandoConexao},
            {(IntegrationStatus.TestandoConexao,    Sucesso),         IntegrationStatus.Conectada},
            {(IntegrationStatus.TestandoConexao,    Falha),           IntegrationStatus.ErroConexao},

            {(IntegrationStatus.Conectada,          Sincronizar),     IntegrationStatus.Sincronizando},
            {(IntegrationStatus.Sincronizando,      SyncSucesso),     IntegrationStatus.Conectada},
            {(IntegrationStatus.Sincronizando,      SyncErro),        IntegrationStatus.FalhaSincronizacao},
            {(IntegrationStatus.FalhaSincronizacao, TentarNovamente), IntegrationStatus.Sincronizando},
        };

        public static IntegrationStatus Aplicar(IntegrationStatus atual, string evento)
        {
            // Jira respondeu 401 (token expirado ou revogado): qualquer estado -> ErroConexao (RN26).
            if (evento == TokenExpirado)
            {
                return IntegrationStatus.ErroConexao;
            }

            return T.TryGetValue((atual, evento), out var novo)
                ? novo
                : throw new BusinessRuleException(409, Messages.RN22_Transicao(atual, evento));
        }

        public static bool Permite(IntegrationStatus atual, string evento) =>
            evento == TokenExpirado || T.ContainsKey((atual, evento));
    }
}
