using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T22 — relatórios PDF e Excel (RF43, RF44).
    public class ReportTests : ApiTestBase
    {
        public ReportTests(ApiFactory factory) : base(factory) { }

        private async Task<(User Gerente, Portfolio Portfolio)> CenarioAsync()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente, PortfolioStatus.Monitoramento);
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            await Factory.WithDbAsync(async db =>
            {
                db.PriorityCriterias.Add(new PriorityCriteria { Name = "Valor de negócio", ValueWeight = 4, PortfolioId = portfolio.Id, UserId = gerente.Id });
                db.Projects.Add(new Project
                {
                    Name = "Portal do Cliente", PortfolioId = portfolio.Id, OwnerId = gerente.Id, Status = ProjectStatus.EmAndamento,
                    StrategicCategory = StrategicCategory.Grow, StartDate = hoje.AddDays(-30), EndDate = hoje.AddDays(60), ApprovedBudget = 10_000,
                });
                await db.SaveChangesAsync();
            });
            return (gerente, portfolio);
        }

        [Theory]
        [InlineData("portfolio", "pdf", "application/pdf")]
        [InlineData("executivo", "pdf", "application/pdf")]
        [InlineData("portfolio", "excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [InlineData("executivo", "excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task GeraOs4Arquivos_EGravaHistorico(string tipo, string formato, string contentType)
        {
            var (gerente, portfolio) = await CenarioAsync();
            var client = Factory.ClientFor(gerente);

            var response = await client.GetAsync($"/api/portfolios/{portfolio.Id}/relatorios/{tipo}?formato={formato}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(contentType, response.Content.Headers.ContentType!.MediaType);
            var nome = response.Content.Headers.ContentDisposition!.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName!;
            Assert.StartsWith($"prumo-{tipo}-portfolio-", nome.Trim('"'));
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (formato == "pdf")
            {
                Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
            }
            else
            {
                using var workbook = new XLWorkbook(new MemoryStream(bytes));
                Assert.Contains(workbook.Worksheets, w => w.Name == "Resumo");
                if (tipo == "portfolio")
                {
                    var projetos = workbook.Worksheet("Projetos");
                    Assert.Equal("Portal do Cliente", projetos.Cell(2, 1).GetString());
                    Assert.Equal("Valor de negócio", workbook.Worksheet("Critérios de priorização").Cell(2, 1).GetString());
                }
                else
                {
                    Assert.Equal(9, workbook.Worksheet("Indicadores").LastRowUsed()!.RowNumber()); // cabeçalho + 8 indicadores
                }
            }

            var historico = await client.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/relatorios/historico");
            Assert.Equal(1, historico.GetArrayLength());
            Assert.Equal(formato == "pdf" ? "PDF" : "Excel", historico[0].GetProperty("formato").GetString());
            Assert.Equal(tipo == "portfolio" ? "Portfolio" : "Executivo", historico[0].GetProperty("tipo").GetString());
        }

        [Fact]
        public async Task PerfilSemPermissao_403_EFormatoInvalido_400()
        {
            var (gerente, portfolio) = await CenarioAsync();
            var dev = await UserAsync(RoleName.Desenvolvedor);

            var semPerfil = await Factory.ClientFor(dev).GetAsync($"/api/portfolios/{portfolio.Id}/relatorios/portfolio?formato=pdf");
            var formato = await Factory.ClientFor(gerente).GetAsync($"/api/portfolios/{portfolio.Id}/relatorios/portfolio?formato=doc");

            Assert.Equal(HttpStatusCode.Forbidden, semPerfil.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, formato.StatusCode);
        }
    }
}
