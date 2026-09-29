using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    /// <summary>
    /// RegrasNotificacaoService (RF45, T21): executado todo dia às 07:00 e depois de cada sincronização
    /// com o Jira. A deduplicação de 24 h fica em <see cref="INotificationService.CreateAsync"/>.
    /// </summary>
    public class NotificationRulesService : INotificationRulesService, ISyncCompletedHandler
    {
        public const string EntidadeProjeto = "Projeto";
        public const string EntidadeEquipe = "Equipe";
        public const string EntidadeDependencia = "Dependencia";

        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private readonly IAppDbContext _db;
        private readonly IndicatorDataLoader _loader;
        private readonly INotificationService _notifications;

        public NotificationRulesService(IAppDbContext db, IndicatorDataLoader loader, INotificationService notifications)
        {
            _db = db;
            _loader = loader;
            _notifications = notifications;
        }

        public Task OnSyncCompletedAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);

        public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var criadas = 0;

            // Portfólios encerrados são somente leitura: não geram notificações.
            var portfolios = await _db.Portfolios.AsNoTracking()
                .Where(p => p.Status != PortfolioStatus.Encerrado)
                .Select(p => new { p.Id, p.OwnerId })
                .ToListAsync(cancellationToken);
            var portfolioOwner = portfolios.ToDictionary(p => p.Id, p => p.OwnerId);
            var portfolioIds = portfolioOwner.Keys.ToList();

            var activeUsers = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => new { u.Id, Roles = u.Roles.Select(r => r.Role).ToList() })
                .ToDictionaryAsync(u => u.Id, u => u.Roles, cancellationToken);
            var membersByPortfolio = (await _db.PortfolioMembers.AsNoTracking()
                    .Where(m => portfolioIds.Contains(m.PortfolioId))
                    .Select(m => new { m.PortfolioId, m.UserId })
                    .ToListAsync(cancellationToken))
                .ToLookup(m => m.PortfolioId, m => m.UserId);

            IEnumerable<Guid> MembrosComPerfil(Guid portfolioId, RoleName role) =>
                membersByPortfolio[portfolioId].Where(u => activeUsers.TryGetValue(u, out var roles) && roles.Contains(role));

            async Task Notificar(IEnumerable<Guid> usuarios, AlertType tipo, string mensagem, string entidade, Guid entidadeId)
            {
                foreach (var usuario in usuarios.Distinct().Where(activeUsers.ContainsKey))
                {
                    if (await _notifications.CreateAsync(usuario, tipo, mensagem, entidade, entidadeId, cancellationToken))
                    {
                        criadas++;
                    }
                }
            }

            var projects = await _db.Projects.AsNoTracking()
                .Where(p => portfolioIds.Contains(p.PortfolioId)
                            && p.Status != ProjectStatus.Cancelado
                            && p.Status != ProjectStatus.Arquivado)
                .ToListAsync(cancellationToken);
            var emAndamento = projects.Where(p => p.Status != ProjectStatus.Concluido).ToList();
            var burnRates = await _loader.BurnRatesAsync(projects, hoje);
            var issues = await _loader.IssuesAsync(emAndamento.Select(p => p.Id).ToList());
            var comOkr = (await _db.ProjectObjectives.AsNoTracking()
                    .Select(po => po.ProjectId)
                    .Distinct()
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            foreach (var p in emAndamento)
            {
                var dono = portfolioOwner[p.PortfolioId];

                // Atraso: hoje > DataFim -> responsável do projeto e do portfólio.
                if (hoje > p.EndDate)
                {
                    await Notificar(new[] { p.OwnerId, dono }, AlertType.Atraso,
                        $"O projeto {p.Name} ultrapassou a data de término ({p.EndDate.ToString("dd/MM/yyyy", PtBr)}).",
                        EntidadeProjeto, p.Id);
                }

                // Atraso: mais de 20% das issues atrasadas -> responsável do projeto.
                var lista = issues.GetValueOrDefault(p.Id) ?? new List<IssueData>();
                var atrasadas = lista.Count(i => !i.Done && i.DueDate.HasValue && i.DueDate.Value < hoje);
                if (lista.Count > 0 && (decimal)atrasadas / lista.Count > 0.20m)
                {
                    await Notificar(new[] { p.OwnerId }, AlertType.Atraso,
                        $"O projeto {p.Name} tem {atrasadas} tarefas atrasadas.", EntidadeProjeto, p.Id);
                }

                // Risco: Status == EmRisco -> responsável do portfólio.
                if (p.Status == ProjectStatus.EmRisco)
                {
                    await Notificar(new[] { dono }, AlertType.Risco, $"O projeto {p.Name} está em risco.", EntidadeProjeto, p.Id);
                }
            }

            foreach (var p in projects)
            {
                // Desalinhamento: projeto ativo sem OKR -> membros do portfólio com perfil ProductOwner.
                if (!comOkr.Contains(p.Id))
                {
                    await Notificar(MembrosComPerfil(p.PortfolioId, RoleName.ProductOwner), AlertType.Desalinhamento,
                        $"O projeto {p.Name} não está associado a nenhum OKR.", EntidadeProjeto, p.Id);
                }

                // EstouroOrcamento: estouro ou riscoEstouro (F5) -> responsável do projeto e perfil Diretoria.
                if (burnRates.TryGetValue(p.Id, out var burn) && burn.Disponivel && (burn.Estouro || burn.RiscoEstouro))
                {
                    var diretoria = activeUsers.Where(u => u.Value.Contains(RoleName.Diretoria)).Select(u => u.Key);
                    await Notificar(diretoria.Prepend(p.OwnerId), AlertType.EstouroOrcamento,
                        $"O projeto {p.Name} consumiu {(burn.PercentualConsumido ?? 0).ToString("0.##", PtBr)}% do orçamento.",
                        EntidadeProjeto, p.Id);
                }
            }

            // Risco: dependência em risco (F13) -> responsável do projeto de origem.
            var emAndamentoIds = emAndamento.Select(p => p.Id).ToList();
            var dependencias = await _db.ProjectDependencies.AsNoTracking()
                .Where(d => emAndamentoIds.Contains(d.ProjectId))
                .Select(d => new
                {
                    d.Id,
                    OwnerId = d.Project.OwnerId,
                    Origem = new DependencyProject(d.Project.Id, d.Project.Name, d.Project.Status, d.Project.EndDate),
                    Destino = new DependencyProject(d.DependsOnProject.Id, d.DependsOnProject.Name, d.DependsOnProject.Status, d.DependsOnProject.EndDate),
                })
                .ToListAsync(cancellationToken);
            foreach (var d in dependencias)
            {
                var risco = DependencyRiskCalculator.Avaliar(d.Origem, d.Destino);
                if (risco.EmRisco)
                {
                    await Notificar(new[] { d.OwnerId }, AlertType.Risco,
                        $"O projeto {d.Origem.Name} depende de {d.Destino.Name}: {risco.Motivo}.", EntidadeDependencia, d.Id);
                }
            }

            // Conflito: equipe com ocupação > 100% (F9, mês corrente) -> TechLeads dos portfólios da equipe.
            var teams = await _db.Teams.AsNoTracking().Include(t => t.Members).ToListAsync(cancellationToken);
            foreach (var team in teams)
            {
                var capacidade = await _loader.CapacityAsync(team, hoje.Year, hoje.Month);
                if (!capacidade.Disponivel || capacidade.Ocupacao <= 100)
                {
                    continue;
                }

                var destinos = await TeamPortfoliosAsync(team, portfolioIds, cancellationToken);
                await Notificar(destinos.SelectMany(pid => MembrosComPerfil(pid, RoleName.TechLead)), AlertType.Conflito,
                    $"A equipe {team.Name} está com ocupação de {capacidade.Ocupacao.ToString("0.##", PtBr)}%.",
                    EntidadeEquipe, team.Id);
            }

            return criadas;
        }

        /// <summary>Portfólio da equipe; sem vínculo, os portfólios onde os membros são responsáveis por issues.</summary>
        private async Task<List<Guid>> TeamPortfoliosAsync(Team team, IReadOnlyCollection<Guid> portfolioIds, CancellationToken cancellationToken)
        {
            if (team.PortfolioId.HasValue)
            {
                return portfolioIds.Contains(team.PortfolioId.Value) ? new List<Guid> { team.PortfolioId.Value } : new List<Guid>();
            }

            var emails = team.Members.Select(m => m.Email).ToList();
            return await _db.Issues.AsNoTracking()
                .Where(i => i.AssigneeEmail != null && emails.Contains(i.AssigneeEmail) && portfolioIds.Contains(i.Project.PortfolioId))
                .Select(i => i.Project.PortfolioId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
    }
}
