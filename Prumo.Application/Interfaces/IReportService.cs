using Prumo.Application.DTOs.Report;

namespace Prumo.Application.Interfaces
{
    /// <summary>Relatórios do portfólio e executivo (RF43, RF44).</summary>
    public interface IReportService
    {
        /// <summary>tipo: "portfolio" ou "executivo"; formato: "pdf" ou "excel". Grava o histórico.</summary>
        Task<ReportFile> GenerateAsync(Guid portfolioId, string tipo, string? formato);

        Task<IEnumerable<RelatorioDto>> HistoryAsync(Guid portfolioId);
    }

    /// <summary>Gera os bytes do arquivo (QuestPDF / ClosedXML, na Infrastructure).</summary>
    public interface IReportRenderer
    {
        byte[] RenderPdf(ReportDocument document);

        byte[] RenderExcel(ReportDocument document);
    }
}
