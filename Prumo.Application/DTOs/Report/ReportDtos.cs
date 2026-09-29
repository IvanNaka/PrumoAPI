namespace Prumo.Application.DTOs.Report
{
    /// <summary>Conteúdo de um relatório, independente do formato (PDF ou Excel).</summary>
    public class ReportDocument
    {
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Cabeçalho: pares rótulo/valor (nome, objetivo, responsável, status, data de geração).</summary>
        public List<KeyValuePair<string, string>> Cabecalho { get; set; } = new();

        public List<ReportSection> Secoes { get; set; } = new();
    }

    /// <summary>Uma seção = uma tabela no PDF e uma aba no Excel.</summary>
    public class ReportSection
    {
        public string Titulo { get; set; } = string.Empty;
        public List<string> Colunas { get; set; } = new();
        public List<List<string>> Linhas { get; set; } = new();

        /// <summary>Texto exibido quando não há linhas.</summary>
        public string Vazio { get; set; } = "Nenhum registro.";
    }

    public record ReportFile(byte[] Content, string ContentType, string FileName);

    public class RelatorioDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Formato { get; set; } = string.Empty;
        public Guid GeradoPorId { get; set; }
        public string GeradoPorNome { get; set; } = string.Empty;
        public DateTime DataGeracao { get; set; }
    }
}
