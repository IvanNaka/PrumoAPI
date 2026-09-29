using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Dependency;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;

namespace Prumo.Application.Services
{
    // Dependências entre projetos (RF26–RF28, UC12, F13, F14).
    public class ProjectDependencyService : IProjectDependencyService
    {
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;
        private readonly ICurrentUserService _currentUser;

        public ProjectDependencyService(IAppDbContext db, IPortfolioAccessService access, ICurrentUserService currentUser)
        {
            _db = db;
            _access = access;
            _currentUser = currentUser;
        }

        /// <summary>GET /portfolios/{id}/dependencias — cada item com emRisco e motivo (F13).</summary>
        public async Task<IEnumerable<DependenciaDto>> ListByPortfolioAsync(Guid portfolioId)
        {
            await _access.EnsureAccessAsync(portfolioId);
            return await LoadAsync(_db.ProjectDependencies.Where(d => d.PortfolioId == portfolioId));
        }

        /// <summary>Dependências em que o projeto é origem ou destino (aba "Dependências").</summary>
        public async Task<IEnumerable<DependenciaDto>> ListByProjectAsync(Guid projectId)
        {
            await _access.EnsureProjectAccessAsync(projectId);
            return await LoadAsync(_db.ProjectDependencies.Where(d => d.ProjectId == projectId || d.DependsOnProjectId == projectId));
        }

        public async Task<DependenciaDto> CreateAsync(CriarDependenciaDto dto)
        {
            if (dto.ProjetoOrigemId is null || dto.ProjetoDestinoId is null)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios);
            }

            var origemId = dto.ProjetoOrigemId.Value;
            var destinoId = dto.ProjetoDestinoId.Value;
            var portfolioId = await _access.EnsureProjectAccessAsync(origemId, write: true);

            // RN20: um projeto não pode depender de si mesmo.
            if (origemId == destinoId)
            {
                throw new BusinessRuleException(400, Messages.RN20_AutoDependencia);
            }

            var destinoPortfolio = await _db.Projects.Where(p => p.Id == destinoId).Select(p => (Guid?)p.PortfolioId).SingleOrDefaultAsync()
                ?? throw Messages.NotFound("Projeto não encontrado.");
            if (destinoPortfolio != portfolioId)
            {
                throw new BusinessRuleException(400, "Os dois projetos devem pertencer ao mesmo portfólio.");
            }

            // RN21: dependência repetida.
            if (await _db.ProjectDependencies.AnyAsync(d => d.ProjectId == origemId && d.DependsOnProjectId == destinoId))
            {
                throw new BusinessRuleException(409, Messages.RN21_DependenciaRepetida);
            }

            // F14 / RN19: bloqueia se criar um ciclo.
            var arestas = (await _db.ProjectDependencies.AsNoTracking()
                    .Where(d => d.PortfolioId == portfolioId)
                    .Select(d => new { d.ProjectId, d.DependsOnProjectId })
                    .ToListAsync())
                .ToLookup(d => d.ProjectId, d => d.DependsOnProjectId);
            if (DependencyCycleDetector.CriaCiclo(origemId, destinoId, arestas))
            {
                throw new BusinessRuleException(409, Messages.RN19_Ciclo);
            }

            var descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim();
            var dependency = new ProjectDependency
            {
                ProjectId = origemId,
                DependsOnProjectId = destinoId,
                PortfolioId = portfolioId,
                UserId = _currentUser.RequireUserId(),
                Reason = descricao is { Length: > 500 } ? descricao[..500] : descricao,
            };
            _db.ProjectDependencies.Add(dependency);
            await _db.SaveChangesAsync();

            return (await LoadAsync(_db.ProjectDependencies.Where(d => d.Id == dependency.Id))).Single();
        }

        public async Task DeleteAsync(Guid id)
        {
            var dependency = await _db.ProjectDependencies.SingleOrDefaultAsync(d => d.Id == id)
                ?? throw Messages.NotFound("Dependência não encontrada.");
            await _access.EnsureWriteAccessAsync(dependency.PortfolioId);
            _db.ProjectDependencies.Remove(dependency);
            await _db.SaveChangesAsync();
        }

        /// <summary>Carrega as dependências com o risco (F13) já calculado.</summary>
        public static async Task<List<DependenciaDto>> LoadAsync(IQueryable<ProjectDependency> query)
        {
            var items = await query.AsNoTracking()
                .Include(d => d.Project)
                .Include(d => d.DependsOnProject)
                .ToListAsync();

            return items
                .Select(d =>
                {
                    var risco = DependencyRiskCalculator.Avaliar(
                        new DependencyProject(d.Project.Id, d.Project.Name, d.Project.Status, d.Project.EndDate),
                        new DependencyProject(d.DependsOnProject.Id, d.DependsOnProject.Name, d.DependsOnProject.Status, d.DependsOnProject.EndDate));
                    return new DependenciaDto
                    {
                        Id = d.Id,
                        PortfolioId = d.PortfolioId,
                        ProjetoOrigemId = d.ProjectId,
                        ProjetoOrigemNome = d.Project.Name,
                        ProjetoOrigemStatus = d.Project.Status.ToString(),
                        ProjetoDestinoId = d.DependsOnProjectId,
                        ProjetoDestinoNome = d.DependsOnProject.Name,
                        ProjetoDestinoStatus = d.DependsOnProject.Status.ToString(),
                        Descricao = d.Reason,
                        EmRisco = risco.EmRisco,
                        Motivo = risco.Motivo,
                    };
                })
                .OrderByDescending(d => d.EmRisco)
                .ThenBy(d => d.ProjetoOrigemNome)
                .ToList();
        }
    }
}
