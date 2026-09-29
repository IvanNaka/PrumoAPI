using Prumo.Application.Indicators;

namespace Prumo.Application.DTOs.Project
{
    /// <summary>GET /projetos/{id}/indicadores — F5, F6, F7, F8 e F12 do projeto.</summary>
    public class ProjetoIndicadoresDto
    {
        public BurnRateResult BurnRate { get; set; } = new();
    }
}
