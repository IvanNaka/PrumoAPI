using Prumo.Domain.Enums;

namespace Prumo.Application.Indicators
{
    public record CriterionWeight(Guid Id, decimal Weight, CriteriaType Type);

    public record ProjectScores(Guid ProjectId, Priority Priority, DateTime CreatedDate, IReadOnlyDictionary<Guid, int> Scores);

    public record RankedProject(Guid ProjectId, decimal Score, int Position);

    /// <summary>F1 (score de priorização, RF19) e F2 (ranking, RF20).</summary>
    public static class PrioritizationCalculator
    {
        /// <summary>
        /// F1: n'_c = (tipo_c == Custo) ? (6 - n_c) : n_c; media = Σ(w_c × n'_c) / Σ(w_c);
        /// score = (media - 1) / 4 × 100. Devolve null se faltar nota em algum critério.
        /// </summary>
        public static decimal? Score(IReadOnlyCollection<CriterionWeight> criteria, IReadOnlyDictionary<Guid, int> scores)
        {
            if (criteria.Count == 0 || criteria.Any(c => !scores.ContainsKey(c.Id)))
            {
                return null;
            }

            var totalWeight = criteria.Sum(c => c.Weight);
            if (totalWeight <= 0)
            {
                return null;
            }

            var weighted = criteria.Sum(c =>
            {
                var nota = scores[c.Id];
                var ajustada = c.Type == CriteriaType.Custo ? 6 - nota : nota;
                return c.Weight * ajustada;
            });

            var media = weighted / totalWeight;
            return IndicatorMath.R2((media - 1m) / 4m * 100m);
        }

        /// <summary>
        /// F2: ordena por score decrescente; empate -> Prioridade decrescente; persistindo ->
        /// DataCriacao crescente. Posições de 1 a N. Projetos sem score completo ficam de fora.
        /// </summary>
        public static List<RankedProject> Rank(IReadOnlyCollection<CriterionWeight> criteria, IEnumerable<ProjectScores> projects)
        {
            var scored = projects
                .Select(p => new { p.ProjectId, p.Priority, p.CreatedDate, Score = Score(criteria, p.Scores) })
                .Where(p => p.Score.HasValue);

            return Order(scored, p => p.Score!.Value, p => p.Priority, p => p.CreatedDate)
                .Select((p, index) => new RankedProject(p.ProjectId, p.Score!.Value, index + 1))
                .ToList();
        }

        /// <summary>Critério de ordenação do F2 (score desc, prioridade desc, data de criação asc).</summary>
        public static IOrderedEnumerable<T> Order<T>(
            IEnumerable<T> items, Func<T, decimal> score, Func<T, Priority> priority, Func<T, DateTime> createdDate)
        {
            return items
                .OrderByDescending(score)
                .ThenByDescending(i => (int)priority(i))
                .ThenBy(createdDate);
        }
    }
}
