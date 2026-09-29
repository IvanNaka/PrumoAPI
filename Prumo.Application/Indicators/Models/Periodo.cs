using Prumo.Application.Common;

namespace Prumo.Application.Indicators.Models
{
    /// <summary>Período dos indicadores de fluxo (padrão: últimos 90 dias; a API aceita ?de= e ?ate=).</summary>
    public record Periodo(DateOnly De, DateOnly Ate)
    {
        public const int DiasPadrao = 90;

        /// <summary>Início do dia "De" (UTC).</summary>
        public DateTime Inicio => De.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        /// <summary>Fim do dia "Ate" (UTC).</summary>
        public DateTime Fim => Ate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        public static Periodo Criar(DateOnly? de, DateOnly? ate)
        {
            var fim = ate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var inicio = de ?? fim.AddDays(-DiasPadrao);
            if (inicio > fim)
            {
                throw new BusinessRuleException(400, "A data inicial do período deve ser anterior ou igual à data final.");
            }

            return new Periodo(inicio, fim);
        }
    }
}
