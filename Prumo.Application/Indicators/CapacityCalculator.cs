using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators
{
    public record MemberCapacity(Guid Id, string Name, string Email, int MonthlyCapacityHours);

    public record OpenIssueDemand(string? AssigneeEmail, decimal? EstimateHours, decimal SpentHours, bool Done);

    public record MonthWorklog(string? AuthorEmail, DateOnly Date, decimal Hours);

    public class CapacityResult : IndicatorResult
    {
        public Guid? EquipeId { get; set; }
        public string? EquipeNome { get; set; }
        public Guid? MembroId { get; set; }
        public string? MembroNome { get; set; }
        public string Mes { get; set; } = string.Empty;
        public decimal Capacidade { get; set; }
        public decimal Demanda { get; set; }
        public decimal Ocupacao { get; set; }
        public decimal HorasMes { get; set; }
        public decimal Utilizacao { get; set; }
        public string? Classificacao { get; set; }
        public List<CapacityResult>? Membros { get; set; }
        public List<CapacityResult>? Equipes { get; set; }
    }

    /// <summary>F9 — Capacidade, ocupação e utilização da equipe (RF32, RF39).</summary>
    public static class CapacityCalculator
    {
        public const string Subutilizada = "Subutilizada";
        public const string Adequada = "Adequada";
        public const string Sobrecarregada = "Sobrecarregada";

        /// <summary>&lt; 70 Subutilizada; 70 a 100 Adequada; &gt; 100 Sobrecarregada.</summary>
        public static string Classificar(decimal ocupacao) =>
            ocupacao < 70 ? Subutilizada : ocupacao <= 100 ? Adequada : Sobrecarregada;

        /// <summary>Calcula a equipe inteira e cada membro (mesma fórmula, só com o e-mail do membro).</summary>
        public static CapacityResult CalcularEquipe(
            Guid teamId,
            string teamName,
            IReadOnlyCollection<MemberCapacity> members,
            IReadOnlyCollection<OpenIssueDemand> issues,
            IReadOnlyCollection<MonthWorklog> worklogs,
            int year,
            int month)
        {
            var result = Calcular(members, issues, worklogs, year, month);
            result.EquipeId = teamId;
            result.EquipeNome = teamName;
            result.Membros = members
                .Select(m =>
                {
                    var r = Calcular(new[] { m }, issues, worklogs, year, month);
                    r.MembroId = m.Id;
                    r.MembroNome = m.Name;
                    return r;
                })
                .ToList();
            return result;
        }

        /// <summary>
        /// capacidade = Σ CapacidadeMensalHoras;
        /// demanda = Σ max(EstimativaHoras - HorasRealizadas, 0) das issues não concluídas dos membros;
        /// ocupacao% = demanda / capacidade × 100;
        /// horasMes = Σ worklogs do mês dos membros; utilizacao% = horasMes / capacidade × 100.
        /// </summary>
        public static CapacityResult Calcular(
            IReadOnlyCollection<MemberCapacity> members,
            IReadOnlyCollection<OpenIssueDemand> issues,
            IReadOnlyCollection<MonthWorklog> worklogs,
            int year,
            int month)
        {
            var result = new CapacityResult { Mes = $"{year:D4}-{month:D2}" };
            var emails = members.Select(m => m.Email.Trim().ToLowerInvariant()).ToHashSet();
            var capacidade = (decimal)members.Sum(m => m.MonthlyCapacityHours);

            var demanda = issues
                .Where(i => !i.Done && i.AssigneeEmail != null && emails.Contains(i.AssigneeEmail.Trim().ToLowerInvariant()))
                .Sum(i => Math.Max((i.EstimateHours ?? 0) - i.SpentHours, 0));

            var horasMes = worklogs
                .Where(w => w.Date.Year == year && w.Date.Month == month
                            && w.AuthorEmail != null && emails.Contains(w.AuthorEmail.Trim().ToLowerInvariant()))
                .Sum(w => w.Hours);

            result.Capacidade = IndicatorMath.R2(capacidade);
            result.Demanda = IndicatorMath.R2(demanda);
            result.HorasMes = IndicatorMath.R2(horasMes);

            if (capacidade <= 0)
            {
                return result.Indisponivel<CapacityResult>();
            }

            var ocupacao = demanda / capacidade * 100m;
            result.Ocupacao = IndicatorMath.R2(ocupacao);
            result.Utilizacao = IndicatorMath.R2(horasMes / capacidade * 100m);
            result.Classificacao = Classificar(result.Ocupacao);
            return result;
        }

        /// <summary>Visão do portfólio: soma as equipes e mantém a lista por equipe.</summary>
        public static CapacityResult CalcularPortfolio(IReadOnlyCollection<CapacityResult> equipes, int year, int month)
        {
            var result = new CapacityResult { Mes = $"{year:D4}-{month:D2}", Equipes = equipes.ToList() };
            var capacidade = equipes.Sum(e => e.Capacidade);
            var demanda = equipes.Sum(e => e.Demanda);
            var horas = equipes.Sum(e => e.HorasMes);
            result.Capacidade = IndicatorMath.R2(capacidade);
            result.Demanda = IndicatorMath.R2(demanda);
            result.HorasMes = IndicatorMath.R2(horas);

            if (capacidade <= 0)
            {
                return result.Indisponivel<CapacityResult>();
            }

            result.Ocupacao = IndicatorMath.R2(demanda / capacidade * 100m);
            result.Utilizacao = IndicatorMath.R2(horas / capacidade * 100m);
            result.Classificacao = Classificar(result.Ocupacao);
            return result;
        }
    }
}
