namespace Prumo.Domain.Entities
{
    // Relatorio: histórico de relatórios gerados (RF43, RF44).
    public class Report
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;

        /// <summary>Nome do arquivo gerado.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Tipo: "Portfolio" ou "Executivo".</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Formato: "PDF" ou "Excel".</summary>
        public string Format { get; set; } = string.Empty;

        public Guid GeneratedById { get; set; }
        public User GeneratedBy { get; set; } = null!;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
