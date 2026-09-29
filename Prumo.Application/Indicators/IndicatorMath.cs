namespace Prumo.Application.Indicators
{
    /// <summary>Regras gerais da Seção 3.5 (D07).</summary>
    public static class IndicatorMath
    {
        public const string DadosInsuficientes = "Dados insuficientes para calcular este indicador.";

        /// <summary>Arredonda para 2 casas decimais (MidpointRounding.AwayFromZero).</summary>
        public static decimal R2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static decimal R2(double value) =>
            double.IsFinite(value) ? Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero) : 0m;

        public static decimal? R2(decimal? value) => value.HasValue ? R2(value.Value) : null;

        /// <summary>Número de dias entre duas datas (fim - início).</summary>
        public static int Days(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber;

        /// <summary>Meses inteiros completos entre duas datas (0 se <paramref name="to"/> for anterior).</summary>
        public static int WholeMonthsBetween(DateOnly from, DateOnly to)
        {
            var months = (to.Year - from.Year) * 12 + to.Month - from.Month;
            if (to.Day < from.Day)
            {
                months--;
            }

            return Math.Max(0, months);
        }

        /// <summary>Mediana: N ímpar -> elemento do meio; N par -> média dos dois do meio.</summary>
        public static double Median(IReadOnlyList<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var n = sorted.Count;
            if (n == 0)
            {
                return 0;
            }

            return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        }

        /// <summary>Percentil com interpolação linear entre os pontos (método "inclusivo").</summary>
        public static double Percentile(IReadOnlyList<double> values, double percentile)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0)
            {
                return 0;
            }

            var position = (sorted.Count - 1) * percentile / 100.0;
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);
            return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        }
    }
}
