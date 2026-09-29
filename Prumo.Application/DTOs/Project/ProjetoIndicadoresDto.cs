using Prumo.Application.Indicators;

namespace Prumo.Application.DTOs.Project
{
    /// <summary>GET /projetos/{id}/indicadores — F5, F6, F7, F8 e F12 do projeto.</summary>
    public class ProjetoIndicadoresDto
    {
        public DateOnly De { get; set; }
        public DateOnly Ate { get; set; }
        public BurnRateResult BurnRate { get; set; } = new();
        public VplResult Vpl { get; set; } = new();
        public LeadTimeResult LeadTime { get; set; } = new();
        public QualityResult Qualidade { get; set; } = new();
        public ProjectHealth Saude { get; set; } = new();
    }
}
