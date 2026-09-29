using Prumo.Application.Common;
using Prumo.Domain.Enums;

namespace Prumo.Application.StateMachines
{
    /// <summary>
    /// Ciclo de vida do portfólio (Figura 27, Seção 3.4.2). Qualquer transição fora da tabela
    /// devolve RN22.
    /// </summary>
    public static class PortfolioStateMachine
    {
        // Eventos automáticos
        public const string PrimeiroCriterio = "PrimeiroCriterio";
        public const string PrimeiroProjeto = "PrimeiroProjeto";
        public const string Priorizar = "Priorizar";
        public const string AlterarCriterio = "AlterarCriterio";

        // Ações manuais (POST /api/portfolios/{id}/acoes/{acao})
        public const string Aprovar = "Aprovar";
        public const string Reavaliar = "Reavaliar";
        public const string Encerrar = "Encerrar";

        private static readonly Dictionary<(PortfolioStatus, string), PortfolioStatus> T = new()
        {
            {(PortfolioStatus.Criado,        PrimeiroCriterio), PortfolioStatus.Configurado},
            {(PortfolioStatus.Configurado,   PrimeiroProjeto),  PortfolioStatus.EmAnalise},
            {(PortfolioStatus.EmAnalise,     Priorizar),        PortfolioStatus.Priorizado},
            {(PortfolioStatus.Reavaliacao,   Priorizar),        PortfolioStatus.Priorizado},
            // Com o portfólio em Priorizado, novas priorizações são permitidas e ele continua Priorizado.
            {(PortfolioStatus.Priorizado,    Priorizar),        PortfolioStatus.Priorizado},
            {(PortfolioStatus.Priorizado,    Aprovar),          PortfolioStatus.Monitoramento},
            {(PortfolioStatus.Monitoramento, Reavaliar),        PortfolioStatus.Reavaliacao},
            {(PortfolioStatus.Monitoramento, AlterarCriterio),  PortfolioStatus.Reavaliacao},
            {(PortfolioStatus.Monitoramento, Encerrar),         PortfolioStatus.Encerrado},
        };

        public static PortfolioStatus Aplicar(PortfolioStatus atual, string acao)
            => T.TryGetValue((atual, acao), out var novo)
               ? novo
               : throw new BusinessRuleException(409, Messages.RN22_Transicao(atual, acao));

        /// <summary>Para eventos automáticos: aplica se a transição existir; senão mantém o estado.</summary>
        public static PortfolioStatus TentarAplicar(PortfolioStatus atual, string evento)
            => T.TryGetValue((atual, evento), out var novo) ? novo : atual;

        public static bool Permite(PortfolioStatus atual, string acao) => T.ContainsKey((atual, acao));

        /// <summary>Converte a ação da rota (aprovar/reavaliar/encerrar) no nome do evento.</summary>
        public static string? AcaoDaRota(string acao) => acao.ToLowerInvariant() switch
        {
            "aprovar" => Aprovar,
            "reavaliar" => Reavaliar,
            "encerrar" => Encerrar,
            _ => null,
        };
    }
}
