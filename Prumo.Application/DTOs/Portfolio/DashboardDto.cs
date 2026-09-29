using Prumo.Application.Indicators;

namespace Prumo.Application.DTOs.Portfolio
{
    /// <summary>GET /portfolios/{id}/dashboard — as 8 chaves no formato {disponivel, motivo?, ...}.</summary>
    public class DashboardDto
    {
        public DateOnly De { get; set; }
        public DateOnly Ate { get; set; }

        /// <summary>Data da última sincronização do Jira, exibida no topo do dashboard.</summary>
        public DateTime? UltimaSincronizacaoJira { get; set; }

        public BurnRateResult BurnRate { get; set; } = new();
        public HealthResult Saude { get; set; } = new();
        public AllocationResult AlocacaoEstrategica { get; set; } = new();
        public QualityResult Qualidade { get; set; } = new();
        public CapacityResult Capacidade { get; set; } = new();
        public LeadTimeResult LeadTime { get; set; } = new();
        public VplResult Vpl { get; set; } = new();
        public OkrAlignmentResult AlinhamentoOkr { get; set; } = new();
    }
}
