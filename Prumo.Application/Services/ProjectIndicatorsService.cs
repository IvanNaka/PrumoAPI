using Microsoft.EntityFrameworkCore;
using Prumo.Application.DTOs.Project;
using Prumo.Application.Indicators;
using Prumo.Application.Indicators.Models;
using Prumo.Application.Indicators.Portfolio;
using Prumo.Application.Interfaces;

namespace Prumo.Application.Services
{
    // Indicadores de um projeto (roteiro do Cap. 4, Parte 2): F5, F6, F7, F8 e F12.
    public class ProjectIndicatorsService : IProjectIndicatorsService
    {
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;
        private readonly IndicatorDataLoader _loader;

        public ProjectIndicatorsService(IAppDbContext db, IPortfolioAccessService access, IndicatorDataLoader loader)
        {
            _db = db;
            _access = access;
            _loader = loader;
        }

        public async Task<ProjetoIndicadoresDto> GetAsync(Guid projectId, DateOnly? de = null, DateOnly? ate = null)
        {
            await _access.EnsureProjectAccessAsync(projectId);
            var periodo = Periodo.Criar(de, ate);
            var project = await _db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            var projects = new[] { project };
            var burnRates = await _loader.BurnRatesAsync(projects, hoje);
            var vpls = await _loader.VplsAsync(projects, burnRates);
            var issues = (await _loader.IssuesAsync(new[] { projectId })).GetValueOrDefault(projectId) ?? new List<IssueData>();

            // F12: só projetos Planejado, EmAndamento ou EmRisco são avaliados.
            ProjectHealth saude;
            if (HealthCalculator.Avaliavel(project.Status))
            {
                var input = (await _loader.HealthInputsAsync(projects, hoje)).Single();
                saude = HealthCalculator.CalcularProjeto(input, hoje);
            }
            else
            {
                saude = new ProjectHealth { ProjetoId = project.Id, Nome = project.Name, Status = project.Status.ToString() }
                    .Indisponivel<ProjectHealth>();
            }

            return new ProjetoIndicadoresDto
            {
                De = periodo.De,
                Ate = periodo.Ate,
                BurnRate = burnRates[projectId],
                Vpl = vpls[projectId],
                LeadTime = LeadTimeIndicator.CalcularLeadTimeMediano(issues, periodo),
                Qualidade = QualityCalculator.Calcular(issues, periodo.Inicio, periodo.Fim),
                Saude = saude,
            };
        }
    }
}
