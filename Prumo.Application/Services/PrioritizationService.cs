using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Prioritization;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;
using Prumo.Application.StateMachines;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // PriorizacaoService — UC10, RF18–RF21, Figura 28, F1 (score), F2 (ranking) e F3 (recálculo).
    public class PrioritizationService : IPrioritizationService
    {
        private static readonly EvaluationStatus[] Recalculaveis =
            { EvaluationStatus.Priorizado, EvaluationStatus.Aprovado, EvaluationStatus.Reavaliado };

        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;
        private readonly ICurrentUserService _currentUser;

        public PrioritizationService(IAppDbContext db, IPortfolioAccessService access, ICurrentUserService currentUser)
        {
            _db = db;
            _access = access;
            _currentUser = currentUser;
        }

        public async Task RecalculateIfNeededAsync(Guid portfolioId)
        {
            var (criteria, projects) = await LoadAsync(portfolioId);

            foreach (var project in projects.Where(p => Recalculaveis.Contains(p.EvaluationStatus)))
            {
                var score = PrioritizationCalculator.Score(criteria, ScoresOf(project));
                if (score.HasValue)
                {
                    project.CurrentScore = score;
                    // Reavaliado com as notas completas de novo -> recalcula e volta a Priorizado.
                    project.EvaluationStatus = EvaluationStateMachine.TentarAplicar(project.EvaluationStatus, EvaluationStateMachine.NotasCompletas);
                }
                else if (project.EvaluationStatus == EvaluationStatus.Reavaliado)
                {
                    // Aguardando as notas do(s) critério(s) novo(s): sai do ranking até ser reavaliado.
                    project.CurrentScore = null;
                }
            }

            ApplyRanking(projects);
            await _db.SaveChangesAsync();
        }

        public async Task<AvaliacaoProjetoDto> SaveEvaluationsAsync(Guid projectId, IReadOnlyCollection<NotaInputDto> notas)
        {
            var portfolioId = await _access.EnsureProjectAccessAsync(projectId, write: true);
            var project = await _db.Projects.Include(p => p.ProjectEvaluations).SingleAsync(p => p.Id == projectId);

            if (ProjectStateMachine.EhFinal(project.Status))
            {
                throw new BusinessRuleException(409, Messages.RN22_Transicao(project.Status, "Avaliar"));
            }

            if (notas == null || notas.Count == 0)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios);
            }

            // RN17: nota de 1 a 5.
            if (notas.Any(n => n.Nota is null or < 1 or > 5))
            {
                throw new BusinessRuleException(400, Messages.RN17_NotaInvalida);
            }

            var criteriaIds = await _db.PriorityCriterias
                .Where(c => c.PortfolioId == portfolioId)
                .Select(c => c.Id)
                .ToListAsync();
            if (notas.Any(n => !criteriaIds.Contains(n.CriterioId)))
            {
                throw Messages.NotFound(Messages.RN10_CriterioNaoEncontrado);
            }

            var userId = _currentUser.RequireUserId();
            foreach (var nota in notas)
            {
                var existing = project.ProjectEvaluations.SingleOrDefault(e => e.PriorityCriteriaId == nota.CriterioId);
                if (existing == null)
                {
                    var evaluation = new ProjectEvaluation
                    {
                        ProjectId = projectId,
                        PriorityCriteriaId = nota.CriterioId,
                        UserId = userId,
                        Score = nota.Nota!.Value,
                        EvaluatedAt = DateTime.UtcNow,
                    };
                    _db.ProjectEvaluations.Add(evaluation);
                    project.ProjectEvaluations.Add(evaluation);
                }
                else
                {
                    existing.Score = nota.Nota!.Value;
                    existing.UserId = userId;
                    existing.EvaluatedAt = DateTime.UtcNow;
                    existing.UpdatedDate = DateTime.UtcNow;
                }
            }

            // Figura 28: primeira nota gravada -> Avaliando.
            project.EvaluationStatus = EvaluationStateMachine.TentarAplicar(project.EvaluationStatus, EvaluationStateMachine.PrimeiraNota);
            await _db.SaveChangesAsync();

            // RF21 / F3: gravação de nota recalcula o ranking (Reavaliado completo volta a Priorizado).
            await RecalculateIfNeededAsync(portfolioId);

            return await GetProjectEvaluationAsync(projectId);
        }

        public async Task<PriorizacaoResultadoDto> PrioritizeAsync(Guid portfolioId)
        {
            await _access.EnsureWriteAccessAsync(portfolioId);
            var portfolio = await _db.Portfolios.SingleAsync(p => p.Id == portfolioId);
            var (criteria, projects) = await LoadAsync(portfolioId);

            if (criteria.Count == 0)
            {
                throw new BusinessRuleException(409, Messages.RN15_SemCriterios);
            }

            var rankeaveis = projects.Where(p => !ProjectStateMachine.EhFinal(p.Status)).ToList();
            if (rankeaveis.Count == 0)
            {
                throw new BusinessRuleException(409, Messages.RN16_SemProjetos);
            }

            // Figura 27: EmAnalise / Reavaliacao / Priorizado -> Priorizado (senão RN22).
            portfolio.Status = PortfolioStateMachine.Aplicar(portfolio.Status, PortfolioStateMachine.Priorizar);
            portfolio.UpdatedDate = DateTime.UtcNow;

            var now = DateTime.UtcNow;
            foreach (var project in rankeaveis)
            {
                // F1: só recebe score quem tem nota em todos os critérios do portfólio.
                var score = PrioritizationCalculator.Score(criteria, ScoresOf(project));
                if (score.HasValue)
                {
                    project.CurrentScore = score;
                    project.LastPrioritizationDate = now;
                    project.EvaluationStatus = EvaluationStateMachine.TentarAplicar(project.EvaluationStatus, EvaluationStateMachine.Priorizar);
                }
                else
                {
                    project.CurrentScore = null;
                }
            }

            ApplyRanking(projects);
            await _db.SaveChangesAsync();
            return await GetRankingAsync(portfolioId);
        }

        public async Task<PriorizacaoResultadoDto> GetRankingAsync(Guid portfolioId)
        {
            await _access.EnsureAccessAsync(portfolioId);
            var portfolio = await _db.Portfolios.AsNoTracking().SingleAsync(p => p.Id == portfolioId);
            var criteria = await _db.PriorityCriterias.AsNoTracking()
                .Where(c => c.PortfolioId == portfolioId)
                .OrderBy(c => c.Name)
                .ToListAsync();
            var projects = await _db.Projects.AsNoTracking()
                .Include(p => p.ProjectEvaluations)
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();

            var result = new PriorizacaoResultadoDto
            {
                PortfolioStatus = portfolio.Status.ToString(),
                DataUltimaPriorizacao = projects.Max(p => p.LastPrioritizationDate),
                Ranking = projects
                    .Where(p => p.RankingPosition.HasValue && p.CurrentScore.HasValue)
                    .OrderBy(p => p.RankingPosition)
                    .Select(p => new RankingItemDto
                    {
                        Posicao = p.RankingPosition!.Value,
                        ProjetoId = p.Id,
                        Nome = p.Name,
                        Score = p.CurrentScore!.Value,
                        Prioridade = p.Priority.ToString(),
                        Categoria = p.StrategicCategory.ToString(),
                        Status = p.Status.ToString(),
                        StatusAvaliacao = p.EvaluationStatus.ToString(),
                    })
                    .ToList(),
            };

            // Projetos ativos sem nota em todos os critérios, com os critérios que faltam.
            foreach (var project in projects.Where(p => !ProjectStateMachine.EhFinal(p.Status)).OrderBy(p => p.Name))
            {
                var avaliados = project.ProjectEvaluations.Select(e => e.PriorityCriteriaId).ToHashSet();
                var faltantes = criteria.Where(c => !avaliados.Contains(c.Id)).Select(c => c.Name).ToList();
                if (faltantes.Count > 0 || criteria.Count == 0)
                {
                    result.NaoAvaliados.Add(new NaoAvaliadoDto
                    {
                        ProjetoId = project.Id,
                        Nome = project.Name,
                        StatusAvaliacao = project.EvaluationStatus.ToString(),
                        CriteriosFaltantes = faltantes,
                    });
                }
            }

            return result;
        }

        public async Task<MatrizAvaliacaoDto> GetMatrixAsync(Guid portfolioId)
        {
            await _access.EnsureAccessAsync(portfolioId);
            var criteria = await _db.PriorityCriterias.AsNoTracking()
                .Where(c => c.PortfolioId == portfolioId)
                .OrderBy(c => c.Name)
                .Select(c => new CriterioColunaDto { Id = c.Id, Nome = c.Name, Peso = c.ValueWeight, Tipo = c.Type.ToString() })
                .ToListAsync();
            var projects = await _db.Projects.AsNoTracking()
                .Include(p => p.ProjectEvaluations)
                .Where(p => p.PortfolioId == portfolioId && p.Status != ProjectStatus.Cancelado && p.Status != ProjectStatus.Arquivado)
                .OrderBy(p => p.RankingPosition ?? int.MaxValue).ThenBy(p => p.Name)
                .ToListAsync();

            return new MatrizAvaliacaoDto
            {
                Criterios = criteria,
                Projetos = projects.Select(p => new AvaliacaoProjetoLinhaDto
                {
                    ProjetoId = p.Id,
                    Nome = p.Name,
                    Status = p.Status.ToString(),
                    StatusAvaliacao = p.EvaluationStatus.ToString(),
                    ScoreAtual = p.CurrentScore,
                    Notas = p.ProjectEvaluations.ToDictionary(e => e.PriorityCriteriaId, e => e.Score),
                }).ToList(),
            };
        }

        public async Task<AvaliacaoProjetoDto> DecideAsync(Guid projectId, string acao)
        {
            var portfolioId = await _access.EnsureProjectAccessAsync(projectId, write: true);
            var project = await _db.Projects.SingleAsync(p => p.Id == projectId);

            var evento = acao.Trim().ToLowerInvariant() switch
            {
                "aprovar" => EvaluationStateMachine.Aprovar,
                "rejeitar" => EvaluationStateMachine.Rejeitar,
                _ => throw new BusinessRuleException(409, Messages.RN22_Transicao(project.EvaluationStatus, acao)),
            };

            // RN29: somente projetos priorizados podem ser aprovados ou rejeitados.
            if (project.EvaluationStatus != EvaluationStatus.Priorizado)
            {
                throw new BusinessRuleException(409, Messages.RN29_SomentePriorizados);
            }

            project.EvaluationStatus = EvaluationStateMachine.Aplicar(project.EvaluationStatus, evento);

            // Aprovar a avaliação também aprova o projeto quando ele está em Rascunho (3.4.3).
            if (evento == EvaluationStateMachine.Aprovar && project.Status == ProjectStatus.Rascunho)
            {
                project.Status = ProjectStateMachine.Aplicar(project.Status, "Aprovar");
            }

            project.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await RecalculateIfNeededAsync(portfolioId);
            return await GetProjectEvaluationAsync(projectId);
        }

        private async Task<AvaliacaoProjetoDto> GetProjectEvaluationAsync(Guid projectId)
        {
            var project = await _db.Projects.AsNoTracking().Include(p => p.ProjectEvaluations).SingleAsync(p => p.Id == projectId);
            var criteria = await _db.PriorityCriterias.AsNoTracking()
                .Where(c => c.PortfolioId == project.PortfolioId)
                .Select(c => c.Id)
                .ToListAsync();
            return new AvaliacaoProjetoDto
            {
                ProjetoId = project.Id,
                StatusAvaliacao = project.EvaluationStatus.ToString(),
                ScoreAtual = project.CurrentScore,
                PosicaoRanking = project.RankingPosition,
                Completa = criteria.Count > 0 && criteria.All(c => project.ProjectEvaluations.Any(e => e.PriorityCriteriaId == c)),
                Notas = project.ProjectEvaluations
                    .Select(e => new NotaDto { CriterioId = e.PriorityCriteriaId, Nota = e.Score })
                    .ToList(),
            };
        }

        private async Task<(List<CriterionWeight> Criteria, List<Project> Projects)> LoadAsync(Guid portfolioId)
        {
            var criteria = await _db.PriorityCriterias.AsNoTracking()
                .Where(c => c.PortfolioId == portfolioId)
                .Select(c => new CriterionWeight(c.Id, c.ValueWeight, c.Type))
                .ToListAsync();

            var projects = await _db.Projects
                .Include(p => p.ProjectEvaluations)
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();

            return (criteria, projects);
        }

        private static IReadOnlyDictionary<Guid, int> ScoresOf(Project project) =>
            project.ProjectEvaluations
                .GroupBy(e => e.PriorityCriteriaId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.EvaluatedAt).First().Score);

        /// <summary>
        /// F2: posições de 1 a N para os projetos com score; Cancelado e Arquivado não entram no ranking.
        /// </summary>
        private static void ApplyRanking(List<Project> projects)
        {
            var rankeaveis = projects
                .Where(p => p.CurrentScore.HasValue && !ProjectStateMachine.EhFinal(p.Status))
                .ToList();

            var ranking = PrioritizationCalculator
                .Order(rankeaveis, p => p.CurrentScore!.Value, p => p.Priority, p => p.CreatedDate)
                .ToList();

            foreach (var project in projects)
            {
                project.RankingPosition = null;
            }

            for (var i = 0; i < ranking.Count; i++)
            {
                ranking[i].RankingPosition = i + 1;
            }
        }
    }
}
