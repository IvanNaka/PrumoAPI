using Microsoft.EntityFrameworkCore;
using Prumo.Application.DTOs.Project;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;

namespace Prumo.Application.Services
{
    // Indicadores de um projeto (roteiro do Cap. 4, Parte 2).
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

        public async Task<ProjetoIndicadoresDto> GetAsync(Guid projectId)
        {
            await _access.EnsureProjectAccessAsync(projectId);
            var project = await _db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            var projects = new[] { project };
            var burnRates = await _loader.BurnRatesAsync(projects, hoje);
            var vpls = await _loader.VplsAsync(projects, burnRates);
            return new ProjetoIndicadoresDto { BurnRate = burnRates[projectId], Vpl = vpls[projectId] };
        }
    }
}
