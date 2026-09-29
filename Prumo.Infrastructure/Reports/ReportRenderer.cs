using ClosedXML.Excel;
using Prumo.Application.DTOs.Report;
using Prumo.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Prumo.Infrastructure.Reports
{
    /// <summary>PDF com QuestPDF e Excel com ClosedXML (uma aba por seção).</summary>
    public class ReportRenderer : IReportRenderer
    {
        private const string Azul = "#1D4ED8";

        static ReportRenderer()
        {
            // Também definida no Program.cs; repetida aqui para quem sobe a API sem o Main (ex.: testes).
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] RenderPdf(ReportDocument document)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(t => t.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(document.Titulo).FontSize(16).Bold().FontColor(Azul);
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(150);
                                c.RelativeColumn();
                            });
                            foreach (var (rotulo, valor) in document.Cabecalho)
                            {
                                table.Cell().Text(rotulo).SemiBold();
                                table.Cell().Text(valor);
                            }
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(8).Column(col =>
                    {
                        foreach (var secao in document.Secoes)
                        {
                            col.Item().PaddingTop(12).PaddingBottom(4).Text(secao.Titulo).FontSize(12).Bold();
                            if (secao.Linhas.Count == 0)
                            {
                                col.Item().Text(secao.Vazio).Italic().FontColor(Colors.Grey.Darken1);
                                continue;
                            }

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    foreach (var _ in secao.Colunas) c.RelativeColumn();
                                });
                                table.Header(header =>
                                {
                                    foreach (var coluna in secao.Colunas)
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(coluna).SemiBold();
                                    }
                                });
                                foreach (var linha in secao.Linhas)
                                {
                                    for (var i = 0; i < secao.Colunas.Count; i++)
                                    {
                                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                            .Text(i < linha.Count ? linha[i] : string.Empty);
                                    }
                                }
                            });
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span("Prumo — página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        public byte[] RenderExcel(ReportDocument document)
        {
            using var workbook = new XLWorkbook();

            var resumo = workbook.Worksheets.Add("Resumo");
            resumo.Cell(1, 1).Value = document.Titulo;
            resumo.Cell(1, 1).Style.Font.Bold = true;
            resumo.Cell(1, 1).Style.Font.FontSize = 14;
            var row = 3;
            foreach (var (rotulo, valor) in document.Cabecalho)
            {
                resumo.Cell(row, 1).Value = rotulo;
                resumo.Cell(row, 1).Style.Font.Bold = true;
                resumo.Cell(row, 2).Value = valor;
                row++;
            }
            resumo.Columns().AdjustToContents();

            var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Resumo" };
            foreach (var secao in document.Secoes)
            {
                var sheet = workbook.Worksheets.Add(SheetName(secao.Titulo, nomes));
                for (var c = 0; c < secao.Colunas.Count; c++)
                {
                    var cell = sheet.Cell(1, c + 1);
                    cell.Value = secao.Colunas[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                }

                if (secao.Linhas.Count == 0)
                {
                    sheet.Cell(2, 1).Value = secao.Vazio;
                }

                for (var r = 0; r < secao.Linhas.Count; r++)
                {
                    for (var c = 0; c < secao.Linhas[r].Count; c++)
                    {
                        sheet.Cell(r + 2, c + 1).Value = secao.Linhas[r][c];
                    }
                }

                sheet.SheetView.FreezeRows(1);
                sheet.Columns().AdjustToContents(1, Math.Min(secao.Linhas.Count + 1, 200), 10, 80);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>Nome de aba válido no Excel: até 31 caracteres, sem : \ / ? * [ ] e sem repetir.</summary>
        private static string SheetName(string titulo, HashSet<string> usados)
        {
            var limpo = new string(titulo.Where(c => !":\\/?*[]".Contains(c)).ToArray()).Trim();
            if (limpo.Length > 31) limpo = limpo[..31];
            var nome = limpo.Length == 0 ? "Seção" : limpo;
            var i = 2;
            while (!usados.Add(nome))
            {
                var sufixo = $" ({i++})";
                nome = (limpo.Length + sufixo.Length > 31 ? limpo[..(31 - sufixo.Length)] : limpo) + sufixo;
            }

            return nome;
        }
    }
}
