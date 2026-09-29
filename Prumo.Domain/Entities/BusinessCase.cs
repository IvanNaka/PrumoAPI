namespace Prumo.Domain.Entities
{
    // BusinessCase (RF25): 1 por projeto. Base do VPL esperado (F6).
    public class BusinessCase : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>InvestimentoInicial: maior ou igual a 0.</summary>
        public decimal InitialInvestment { get; set; }

        /// <summary>TaxaDescontoAnual, em %: de 0 a 100.</summary>
        public decimal AnnualDiscountRate { get; set; }

        public ICollection<CashFlowForecast> Flows { get; set; } = new List<CashFlowForecast>();
    }

    // FluxoCaixaPrevisto: Mes = meses após a DataInicio do projeto (≥ 1, único no business case).
    public class CashFlowForecast
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BusinessCaseId { get; set; }
        public BusinessCase BusinessCase { get; set; } = null!;
        public int Month { get; set; }
        public decimal Value { get; set; }
    }

    // RetornoRealizado: retorno financeiro efetivamente obtido (VPL realizado, F6).
    public class RealizedReturn : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public DateOnly Date { get; set; }
        public decimal Value { get; set; }
        public string? Description { get; set; }
    }
}
