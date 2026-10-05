namespace Prumo.Application.Common
{
    /// <summary>
    /// Limites superiores dos valores digitados pelo usuário. Ficam abaixo da capacidade das colunas
    /// (decimal(18,2) e decimal(10,2)) para que um valor exagerado vire 400 em vez de estourar no banco.
    /// </summary>
    public static class Limites
    {
        /// <summary>Teto de valores monetários: orçamento, lançamentos, investimento, fluxos e retornos.</summary>
        public const decimal ValorMonetarioMaximo = 1_000_000_000_000m;

        /// <summary>Teto do custo por hora de um membro de equipe (coluna decimal(10,2)).</summary>
        public const decimal CustoHoraMaximo = 100_000m;

        /// <summary>Teto da meta e do valor atual de um Key Result.</summary>
        public const decimal ValorKeyResultMaximo = 1_000_000_000_000m;

        public const string ValorMonetarioAcimaDoLimite = "O valor informado excede o limite de R$ 1.000.000.000.000,00.";
    }
}
