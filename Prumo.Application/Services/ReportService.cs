using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Report;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    /// <summary>Relatório do portfólio e relatório executivo, em PDF ou Excel (RF43, RF44, T22).</summary>
    public class ReportService : IReportService
    {
        public const string TipoPortfolio = "Portfolio";
        public const string TipoExecutivo = "Executivo";

        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;
        private readonly ICurrentUserService _currentUser;
        private readonly IPrioritizationService _prioritization;
        private readonly IDashboardService _dashboard;
        private readonly IndicatorDataLoader _loader;
        private readonly IReportRenderer _renderer;

        public ReportService(
            IAppDbContext db,
            IPortfolioAccessService access,
            ICurrentUserService currentUser,
            IPrioritizationService prioritization,
            IDashboardService dashboard,
            IndicatorDataLoader loader,
            IReportRenderer renderer)
        {
            _db = db;
            _access = access;
            _currentUser = currentUser;
            _prioritization = prioritization;
            _dashboard = dashboard;
            _loader = loader;
            _renderer = renderer;
        }

        public async Task<ReportFile> GenerateAsync(Guid portfolioId, string tipo, string? formato)
        {
            var tipoNormalizado = tipo.Trim().ToLowerInvariant() switch
            {
                "portfolio" => TipoPortfolio,
                "executivo" => TipoExecutivo,
                _ => throw new BusinessRuleException(404, "Relatório inexistente. Use 'portfolio' ou 'executivo'."),
            };
            var excel = (formato ?? "pdf").Trim().ToLowerInvariant() switch
            {
                "pdf" => false,
                "excel" or "xlsx" => true,
                _ => throw new BusinessRuleException(400, "Formato inválido. Use 'pdf' ou 'excel'."),
            };

            await _access.EnsureAccessAsync(portfolioId);
            var portfolio = await _db.Portfolios.AsNoTracking().Include(p => p.Owner).SingleAsync(p => p.Id == portfolioId);

            var document = tipoNormalizado == TipoPortfolio
                ? await PortfolioReportAsync(portfolio)
                : await ExecutiveReportAsync(portfolio);

            var agora = DateTime.UtcNow;
            var nome = $"prumo-{tipoNormalizado.ToLowerInvariant()}-{Slug(portfolio.Name)}-{agora:yyyyMMdd}.{(excel ? "xlsx" : "pdf")}";
            var bytes = excel ? _renderer.RenderExcel(document) : _renderer.RenderPdf(document);

            _db.Reports.Add(new Report
            {
                PortfolioId = portfolioId,
                Name = nome,
                Type = tipoNormalizado,
                Format = excel ? "Excel" : "PDF",
                GeneratedById = _currentUser.RequireUserId(),
                GeneratedAt = agora,
            });
            await _db.SaveChangesAsync();

            return new ReportFile(
                bytes,
                excel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf",
                nome);
        }

        public async Task<IEnumerable<RelatorioDto>> HistoryAsync(Guid portfolioId)
        {
            await _access.EnsureAccessAsync(portfolioId);
            return await _db.Reports.AsNoTracking()
                .Where(r => r.PortfolioId == portfolioId)
                .OrderByDescending(r => r.GeneratedAt)
                .Take(100)
                .Select(r => new RelatorioDto
                {
                    Id = r.Id,
                    Nome = r.Name,
                    Tipo = r.Type,
                    Formato = r.Format,
                    GeradoPorId = r.GeneratedById,
                    GeradoPorNome = r.GeneratedBy.Name,
                    DataGeracao = r.GeneratedAt,
                })
                .ToListAsync();
        }

        /// <summary>Cabeçalho; critérios; ranking completo; projetos; dependências em risco.</summary>
        private async Task<ReportDocument> PortfolioReportAsync(Portfolio portfolio)
        {
            var document = new ReportDocument { Titulo = $"Relatório do portfólio — {portfolio.Name}", Cabecalho = Cabecalho(portfolio) };

            var criterios = await _db.PriorityCriterias.AsNoTracking()
                .Where(c => c.PortfolioId == portfolio.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();
            document.Secoes.Add(new ReportSection
            {
                Titulo = "Critérios de priorização",
                Colunas = { "Critério", "Tipo", "Peso", "Descrição" },
                Linhas = criterios.Select(c => new List<string>
                {
                    c.Name, c.Type == CriteriaType.Custo ? "Custo" : "Benefício", Num(c.ValueWeight), c.Description ?? "",
                }).ToList(),
                Vazio = "O portfólio não tem critérios cadastrados.",
            });

            var ranking = await _prioritization.GetRankingAsync(portfolio.Id);
            document.Secoes.Add(new ReportSection
            {
                Titulo = "Ranking de priorização",
                Colunas = { "Posição", "Projeto", "Score", "Prioridade", "Categoria", "Status" },
                Linhas = ranking.Ranking.Select(r => new List<string>
                {
                    r.Posicao.ToString(PtBr), r.Nome, Num(r.Score), Rotulo(r.Prioridade), r.Categoria, Rotulo(r.Status),
                }).ToList(),
                Vazio = "Nenhum projeto priorizado.",
            });

            var projects = await _db.Projects.AsNoTracking()
                .Include(p => p.Owner)
                .Include(p => p.ProjectObjectives).ThenInclude(po => po.Objective)
                .Where(p => p.PortfolioId == portfolio.Id)
                .OrderBy(p => p.Name)
                .ToListAsync();
            var burnRates = await _loader.BurnRatesAsync(projects, DateOnly.FromDateTime(DateTime.UtcNow));
            document.Secoes.Add(new ReportSection
            {
                Titulo = "Projetos",
                Colunas = { "Projeto", "Responsável", "Status", "Categoria", "Orçamento", "Custo realizado", "% consumido", "OKRs" },
                Linhas = projects.Select(p =>
                {
                    var burn = burnRates[p.Id];
                    return new List<string>
                    {
                        p.Name,
                        p.Owner?.Name ?? "",
                        Rotulo(p.Status),
                        p.StrategicCategory.ToString(),
                        Brl(p.ApprovedBudget),
                        Brl(burn.CustoRealizado),
                        burn.PercentualConsumido.HasValue ? Num(burn.PercentualConsumido.Value) + "%" : "—",
                        string.Join("; ", p.ProjectObjectives.Select(po => po.Objective.Title).OrderBy(t => t)),
                    };
                }).ToList(),
                Vazio = "Nenhum projeto cadastrado.",
            });

            var dependencias = (await ProjectDependencyService.LoadAsync(_db.ProjectDependencies.Where(d => d.PortfolioId == portfolio.Id)))
                .Where(d => d.EmRisco)
                .ToList();
            document.Secoes.Add(new ReportSection
            {
                Titulo = "Dependências em risco",
                Colunas = { "Projeto", "Depende de", "Status do dependido", "Motivo" },
                Linhas = dependencias.Select(d => new List<string>
                {
                    d.ProjetoOrigemNome, d.ProjetoDestinoNome, Rotulo(d.ProjetoDestinoStatus), d.Motivo ?? "",
                }).ToList(),
                Vazio = "Nenhuma dependência em risco.",
            });

            return document;
        }

        /// <summary>8 indicadores do dashboard; top 5 do ranking; projetos críticos; alertas abertos.</summary>
        private async Task<ReportDocument> ExecutiveReportAsync(Portfolio portfolio)
        {
            var document = new ReportDocument { Titulo = $"Relatório executivo — {portfolio.Name}", Cabecalho = Cabecalho(portfolio) };
            var d = await _dashboard.GetAsync(portfolio.Id, null, null);
            document.Cabecalho.Add(new("Período dos indicadores", $"{d.De.ToString("dd/MM/yyyy", PtBr)} a {d.Ate.ToString("dd/MM/yyyy", PtBr)}"));

            string Ou(Indicators.Models.IndicatorResult r, Func<string> valor) => r.Disponivel ? valor() : r.Motivo ?? IndicatorMath.DadosInsuficientes;

            document.Secoes.Add(new ReportSection
            {
                Titulo = "Indicadores",
                Colunas = { "Indicador", "Valor", "Detalhe" },
                Linhas =
                {
                    new() { "Burn Rate", Ou(d.BurnRate, () => Num(d.BurnRate.PercentualConsumido ?? 0) + "% consumido"),
                        Ou(d.BurnRate, () => $"Realizado {Brl(d.BurnRate.CustoRealizado)} de {Brl(d.BurnRate.OrcamentoAprovado)}; burn mensal {Brl(d.BurnRate.BurnRateMensal)}; projeção {Brl(d.BurnRate.ProjecaoCustoTotal)}") },
                    new() { "Saúde do portfólio", Ou(d.Saude, () => $"{Num(d.Saude.Saude)} ({d.Saude.Classificacao})"),
                        Ou(d.Saude, () => string.Join("; ", d.Saude.Contagem.Select(c => $"{c.Key}: {c.Value}"))) },
                    new() { "Alocação estratégica", Ou(d.AlocacaoEstrategica, () => string.Join("; ", d.AlocacaoEstrategica.Categorias.Select(c => $"{c.Categoria} {Num(c.PercentualOrcamento)}%"))),
                        Ou(d.AlocacaoEstrategica, () => "Orçamento total " + Brl(d.AlocacaoEstrategica.OrcamentoTotal)) },
                    new() { "Qualidade da entrega", Ou(d.Qualidade, () => Num(d.Qualidade.IndiceQualidade) + "%"),
                        Ou(d.Qualidade, () => $"{d.Qualidade.Bugs} bugs / {d.Qualidade.Entregas} entregas; razão {Num(d.Qualidade.RazaoBugsPorEntrega)} (meta ≤ {Num(d.Qualidade.MetaRazao)}): {(d.Qualidade.AtingiuMeta ? "meta atingida" : "meta não atingida")}") },
                    new() { "Capacidade das equipes", Ou(d.Capacidade, () => $"{Num(d.Capacidade.Ocupacao)}% ({d.Capacidade.Classificacao})"),
                        Ou(d.Capacidade, () => string.Join("; ", (d.Capacidade.Equipes ?? new()).Select(e => $"{e.EquipeNome} {Num(e.Ocupacao)}%"))) },
                    new() { "Lead time", Ou(d.LeadTime, () => Num(d.LeadTime.LeadTimeMediano) + " dias (mediana)"),
                        Ou(d.LeadTime, () => $"Média {Num(d.LeadTime.LeadTimeMedio)} dias; P85 {Num(d.LeadTime.P85)} dias; {d.LeadTime.Quantidade} issues") },
                    new() { "VPL esperado × realizado", Ou(d.Vpl, () => $"{Brl(d.Vpl.VplEsperado)} × {Brl(d.Vpl.VplRealizado)}"),
                        Ou(d.Vpl, () => d.Vpl.PercentualAtingimento.HasValue ? $"Atingimento {Num(d.Vpl.PercentualAtingimento.Value)}%" : "") },
                    new() { "Alinhamento a OKRs", Ou(d.AlinhamentoOkr, () => Num(d.AlinhamentoOkr.PercentualProjetosAlinhados) + "% dos projetos"),
                        Ou(d.AlinhamentoOkr, () => (d.AlinhamentoOkr.PercentualOrcamentoAlinhado.HasValue ? Num(d.AlinhamentoOkr.PercentualOrcamentoAlinhado.Value) + "% do orçamento; " : "")
                            + "sem OKR: " + (d.AlinhamentoOkr.ListaDesalinhados.Count == 0 ? "nenhum" : string.Join(", ", d.AlinhamentoOkr.ListaDesalinhados.Select(p => p.Nome)))) },
                },
            });

            var ranking = await _prioritization.GetRankingAsync(portfolio.Id);
            document.Secoes.Add(new ReportSection
            {
                Titulo = "Top 5 do ranking",
                Colunas = { "Posição", "Projeto", "Score", "Prioridade", "Categoria" },
                Linhas = ranking.Ranking.Take(5).Select(r => new List<string>
                {
                    r.Posicao.ToString(PtBr), r.Nome, Num(r.Score), Rotulo(r.Prioridade), r.Categoria,
                }).ToList(),
                Vazio = "Nenhum projeto priorizado.",
            });

            document.Secoes.Add(new ReportSection
            {
                Titulo = "Projetos críticos",
                Colunas = { "Projeto", "Status", "Pontuação de saúde", "Motivos" },
                Linhas = d.Saude.Projetos
                    .Where(p => p.Classificacao == HealthCalculator.Critico)
                    .Select(p => new List<string> { p.Nome, Rotulo(p.Status), p.PontuacaoSaude.ToString(PtBr), string.Join("; ", p.Flags) })
                    .ToList(),
                Vazio = "Nenhum projeto crítico.",
            });

            document.Secoes.Add(new ReportSection
            {
                Titulo = "Alertas abertos",
                Colunas = { "Tipo", "Mensagem", "Data" },
                Linhas = await OpenAlertsAsync(portfolio.Id),
                Vazio = "Nenhum alerta aberto.",
            });

            return document;
        }

        /// <summary>
        /// Notificações não arquivadas ligadas aos projetos, dependências e equipes do portfólio
        /// (uma linha por mensagem, mesmo que tenha ido para vários usuários).
        /// </summary>
        private async Task<List<List<string>>> OpenAlertsAsync(Guid portfolioId)
        {
            var projectIds = _db.Projects.Where(p => p.PortfolioId == portfolioId).Select(p => (Guid?)p.Id);
            var dependencyIds = _db.ProjectDependencies.Where(d => d.PortfolioId == portfolioId).Select(d => (Guid?)d.Id);
            var teamIds = _db.Teams.Where(t => t.PortfolioId == portfolioId).Select(t => (Guid?)t.Id);

            var alerts = await _db.Alerts.AsNoTracking()
                .Where(a => a.Status != AlertStatus.Arquivada && a.EntityId != null &&
                            (projectIds.Contains(a.EntityId) || dependencyIds.Contains(a.EntityId) || teamIds.Contains(a.EntityId)))
                .Select(a => new { a.Type, a.Message, a.CreatedDate })
                .ToListAsync();

            return alerts
                .GroupBy(a => new { a.Type, a.Message })
                .Select(g => new { g.Key.Type, g.Key.Message, Data = g.Max(a => a.CreatedDate) })
                .OrderByDescending(a => a.Data)
                .Select(a => new List<string> { Rotulo(a.Type), a.Message, a.Data.ToString("dd/MM/yyyy HH:mm", PtBr) })
                .ToList();
        }

        private static List<KeyValuePair<string, string>> Cabecalho(Portfolio portfolio) => new()
        {
            new("Portfólio", portfolio.Name),
            new("Objetivo", string.IsNullOrWhiteSpace(portfolio.Goal) ? "—" : portfolio.Goal!),
            new("Responsável", portfolio.Owner?.Name ?? "—"),
            new("Status", Rotulo(portfolio.Status)),
            new("Gerado em", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", PtBr) + " (UTC)"),
        };

        /// <summary>Rótulos em português dos valores de enum exibidos nos relatórios.</summary>
        private static readonly Dictionary<string, string> Rotulos = new()
        {
            ["EmAndamento"] = "Em andamento", ["EmRisco"] = "Em risco", ["Concluido"] = "Concluído",
            ["EmAnalise"] = "Em análise", ["Reavaliacao"] = "Reavaliação",
            ["Media"] = "Média", ["Critica"] = "Crítica",
            ["EstouroOrcamento"] = "Estouro de orçamento",
            ["NaoAvaliado"] = "Não avaliado",
        };

        private static string Rotulo(object valor)
        {
            var texto = valor.ToString() ?? string.Empty;
            return Rotulos.TryGetValue(texto, out var rotulo) ? rotulo : texto;
        }

        private static string Num(decimal valor) => valor.ToString("#,##0.##", PtBr);

        private static string Brl(decimal valor) => valor.ToString("C", PtBr);

        /// <summary>Nome do portfólio sem acentos, em minúsculas, com hífens.</summary>
        internal static string Slug(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
            }

            var slug = string.Join('-', sb.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
            return slug.Length == 0 ? "portfolio" : slug.Length > 60 ? slug[..60].TrimEnd('-') : slug;
        }
    }
}
