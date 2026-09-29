using Prumo.Application.Indicators.Models;
using Prumo.Domain.Enums;

namespace Prumo.Application.Indicators
{
    public record BurnRateProject(
        Guid Id,
        string Name,
        ProjectStatus Status,
        DateOnly StartDate,
        DateOnly EndDate,
        DateTime? CompletedAt,
        decimal ApprovedBudget);

    public record WorklogHours(string? AuthorEmail, decimal Hours);

    public class BurnRateResult : IndicatorResult
    {
        public Guid? ProjetoId { get; set; }
        public string? ProjetoNome { get; set; }
        public decimal OrcamentoAprovado { get; set; }
        public decimal CustoHoras { get; set; }
        public decimal CustoLancamentos { get; set; }
        public decimal CustoRealizado { get; set; }
        public decimal HorasSemCusto { get; set; }
        public decimal MesesDecorridos { get; set; }
        public decimal DuracaoMeses { get; set; }
        public decimal BurnRateMensal { get; set; }
        public decimal? PercentualConsumido { get; set; }
        public decimal Saldo { get; set; }
        public decimal ProjecaoCustoTotal { get; set; }
        public bool Estouro { get; set; }
        public bool RiscoEstouro { get; set; }
        public List<BurnRateResult>? Projetos { get; set; }
    }

    /// <summary>F5 — Custo realizado e Burn Rate (RF24, RF36, D13).</summary>
    public static class BurnRateCalculator
    {
        /// <param name="hourlyCostByEmail">custo/hora por e-mail do membro (em minúsculas).</param>
        public static BurnRateResult CalcularProjeto(
            BurnRateProject projeto,
            decimal totalLancamentos,
            IEnumerable<WorklogHours> worklogs,
            IReadOnlyDictionary<string, decimal> hourlyCostByEmail,
            DateOnly hoje)
        {
            // custoHoras = Σ (worklog.Horas × membro.CustoHora); sem membro -> custo 0 e conta em horasSemCusto.
            decimal custoHoras = 0, horasSemCusto = 0;
            foreach (var worklog in worklogs)
            {
                var email = worklog.AuthorEmail?.Trim().ToLowerInvariant();
                if (email != null && hourlyCostByEmail.TryGetValue(email, out var custoHora))
                {
                    custoHoras += worklog.Hours * custoHora;
                }
                else
                {
                    horasSemCusto += worklog.Hours;
                }
            }

            var custoRealizado = custoHoras + totalLancamentos;

            // dataRef = (Status == Concluido) ? DataConclusao : min(hoje, DataFim)
            var dataRef = projeto.Status == ProjectStatus.Concluido && projeto.CompletedAt.HasValue
                ? DateOnly.FromDateTime(projeto.CompletedAt.Value)
                : (hoje < projeto.EndDate ? hoje : projeto.EndDate);

            var mesesDecorridos = Math.Max(1m, IndicatorMath.Days(projeto.StartDate, dataRef) / 30.0m);
            var duracaoMeses = Math.Max(1m, IndicatorMath.Days(projeto.StartDate, projeto.EndDate) / 30.0m);

            // Projeto que ainda não começou: burnRateMensal = 0.
            var burnRateMensal = hoje < projeto.StartDate ? 0m : custoRealizado / mesesDecorridos;
            var projecao = burnRateMensal * duracaoMeses;

            var result = new BurnRateResult
            {
                ProjetoId = projeto.Id,
                ProjetoNome = projeto.Name,
                OrcamentoAprovado = IndicatorMath.R2(projeto.ApprovedBudget),
                CustoHoras = IndicatorMath.R2(custoHoras),
                CustoLancamentos = IndicatorMath.R2(totalLancamentos),
                CustoRealizado = IndicatorMath.R2(custoRealizado),
                HorasSemCusto = IndicatorMath.R2(horasSemCusto),
                MesesDecorridos = IndicatorMath.R2(mesesDecorridos),
                DuracaoMeses = IndicatorMath.R2(duracaoMeses),
                BurnRateMensal = IndicatorMath.R2(burnRateMensal),
                Saldo = IndicatorMath.R2(projeto.ApprovedBudget - custoRealizado),
                ProjecaoCustoTotal = IndicatorMath.R2(projecao),
                Estouro = custoRealizado > projeto.ApprovedBudget,
                RiscoEstouro = projecao > projeto.ApprovedBudget,
            };

            // percentualConsumido = custoRealizado / OrcamentoAprovado × 100 (orçamento = 0 -> indisponível)
            if (projeto.ApprovedBudget <= 0)
            {
                return result.Indisponivel<BurnRateResult>();
            }

            result.PercentualConsumido = IndicatorMath.R2(custoRealizado / projeto.ApprovedBudget * 100m);
            return result;
        }

        /// <summary>
        /// Nível do portfólio: soma custoRealizado e OrcamentoAprovado dos projetos ativos e aplica as
        /// mesmas fórmulas. O burn rate mensal e a projeção do portfólio são a soma dos valores dos projetos.
        /// </summary>
        public static BurnRateResult CalcularPortfolio(IReadOnlyCollection<BurnRateResult> projetos)
        {
            var result = new BurnRateResult { Projetos = projetos.ToList() };
            if (projetos.Count == 0)
            {
                return result.Indisponivel<BurnRateResult>();
            }

            var orcamento = projetos.Sum(p => p.OrcamentoAprovado);
            var custo = projetos.Sum(p => p.CustoRealizado);
            var projecao = projetos.Sum(p => p.ProjecaoCustoTotal);

            result.OrcamentoAprovado = IndicatorMath.R2(orcamento);
            result.CustoHoras = IndicatorMath.R2(projetos.Sum(p => p.CustoHoras));
            result.CustoLancamentos = IndicatorMath.R2(projetos.Sum(p => p.CustoLancamentos));
            result.CustoRealizado = IndicatorMath.R2(custo);
            result.HorasSemCusto = IndicatorMath.R2(projetos.Sum(p => p.HorasSemCusto));
            result.BurnRateMensal = IndicatorMath.R2(projetos.Sum(p => p.BurnRateMensal));
            result.ProjecaoCustoTotal = IndicatorMath.R2(projecao);
            result.Saldo = IndicatorMath.R2(orcamento - custo);
            result.Estouro = custo > orcamento;
            result.RiscoEstouro = projecao > orcamento;

            if (orcamento <= 0)
            {
                return result.Indisponivel<BurnRateResult>();
            }

            result.PercentualConsumido = IndicatorMath.R2(custo / orcamento * 100m);
            return result;
        }
    }
}
