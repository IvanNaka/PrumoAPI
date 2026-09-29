using Microsoft.EntityFrameworkCore;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;
using Prumo.Application.StateMachines;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // PriorizacaoService — F1 (score), F2 (ranking) e F3 (recálculo automático).
    public class PrioritizationService : IPrioritizationService
    {
        private static readonly EvaluationStatus[] Recalculaveis =
            { EvaluationStatus.Priorizado, EvaluationStatus.Aprovado, EvaluationStatus.Reavaliado };

        protected readonly IAppDbContext Db;

        public PrioritizationService(IAppDbContext db)
        {
            Db = db;
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
            await Db.SaveChangesAsync();
        }

        protected async Task<(List<CriterionWeight> Criteria, List<Project> Projects)> LoadAsync(Guid portfolioId)
        {
            var criteria = await Db.PriorityCriterias.AsNoTracking()
                .Where(c => c.PortfolioId == portfolioId)
                .Select(c => new CriterionWeight(c.Id, c.ValueWeight, c.Type))
                .ToListAsync();

            var projects = await Db.Projects
                .Include(p => p.ProjectEvaluations)
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();

            return (criteria, projects);
        }

        protected static IReadOnlyDictionary<Guid, int> ScoresOf(Project project) =>
            project.ProjectEvaluations
                .GroupBy(e => e.PriorityCriteriaId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.EvaluatedAt).First().Score);

        /// <summary>
        /// F2: posições de 1 a N para os projetos com score; Cancelado e Arquivado não entram no ranking.
        /// </summary>
        protected static void ApplyRanking(List<Project> projects)
        {
            var rankeaveis = projects
                .Where(p => p.CurrentScore.HasValue && p.Status is not (ProjectStatus.Cancelado or ProjectStatus.Arquivado))
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
