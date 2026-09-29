using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Project;
using Prumo.Application.Interfaces;
using Prumo.Application.StateMachines;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // Projetos (RF10–RF13, RF22, UC7, Figura 26).
    public class ProjectService : IProjectService
    {
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;
        private readonly IPortfolioService _portfolioService;
        private readonly IPrioritizationService _prioritization;

        public ProjectService(
            IAppDbContext db,
            IPortfolioAccessService access,
            IPortfolioService portfolioService,
            IPrioritizationService prioritization)
        {
            _db = db;
            _access = access;
            _portfolioService = portfolioService;
            _prioritization = prioritization;
        }

        /// <summary>GET /portfolios/{id}/projetos?status=&amp;categoria= — exige ser membro.</summary>
        public async Task<IEnumerable<ProjetoResumoDto>> ListByPortfolioAsync(Guid portfolioId, string? status = null, string? categoria = null)
        {
            await _access.EnsureAccessAsync(portfolioId);
            var query = _db.Projects.AsNoTracking().Where(p => p.PortfolioId == portfolioId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProjectStatus>(status, true, out var s))
            {
                query = query.Where(p => p.Status == s);
            }

            if (!string.IsNullOrWhiteSpace(categoria) && Enum.TryParse<StrategicCategory>(categoria, true, out var c))
            {
                query = query.Where(p => p.StrategicCategory == c);
            }

            var list = await query.Select(p => new ProjetoResumoDto
            {
                Id = p.Id,
                PortfolioId = p.PortfolioId,
                Nome = p.Name,
                Status = p.Status.ToString(),
                ResponsavelId = p.OwnerId,
                ResponsavelNome = p.Owner.Name,
                DataCriacao = p.CreatedDate,
                CategoriaEstrategica = p.StrategicCategory.ToString(),
                Prioridade = p.Priority.ToString(),
                StatusAvaliacao = p.EvaluationStatus.ToString(),
                ScoreAtual = p.CurrentScore,
                PosicaoRanking = p.RankingPosition,
                OrcamentoAprovado = p.ApprovedBudget,
                DataInicio = p.StartDate,
                DataFim = p.EndDate,
            }).ToListAsync();

            return list
                .OrderBy(p => p.PosicaoRanking ?? int.MaxValue)
                .ThenBy(p => p.Nome);
        }

        public async Task<ProjetoDetalheDto> GetDetailAsync(Guid id)
        {
            await _access.EnsureProjectAccessAsync(id);

            var project = await _db.Projects.AsNoTracking()
                .Include(p => p.Owner)
                .Include(p => p.Portfolio)
                .Include(p => p.ProjectEvaluations).ThenInclude(e => e.User)
                .SingleAsync(p => p.Id == id);

            var criteria = await _db.PriorityCriterias.AsNoTracking()
                .Where(c => c.PortfolioId == project.PortfolioId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var okrs = await _db.Objectives.AsNoTracking()
                .Include(o => o.KeyResults)
                .Where(o => o.Projects.Any(po => po.ProjectId == id))
                .OrderBy(o => o.Title)
                .ToListAsync();

            return new ProjetoDetalheDto
            {
                Id = project.Id,
                PortfolioId = project.PortfolioId,
                PortfolioNome = project.Portfolio.Name,
                PortfolioStatus = project.Portfolio.Status.ToString(),
                Nome = project.Name,
                Descricao = project.Description,
                Status = project.Status.ToString(),
                ResponsavelId = project.OwnerId,
                ResponsavelNome = project.Owner.Name,
                DataCriacao = project.CreatedDate,
                CategoriaEstrategica = project.StrategicCategory.ToString(),
                Prioridade = project.Priority.ToString(),
                StatusAvaliacao = project.EvaluationStatus.ToString(),
                ScoreAtual = project.CurrentScore,
                PosicaoRanking = project.RankingPosition,
                OrcamentoAprovado = project.ApprovedBudget,
                DataInicio = project.StartDate,
                DataFim = project.EndDate,
                JiraProjectKey = project.JiraProjectKey,
                DataConclusao = project.CompletedAt,
                DataUltimaPriorizacao = project.LastPrioritizationDate,
                AcoesPermitidas = ProjectStateMachine.AcoesPermitidas(project.Status).ToList(),
                Okrs = okrs.Select(o => new Prumo.Application.DTOs.Okr.OkrResumoDto
                {
                    Id = o.Id,
                    Titulo = o.Title,
                    Progresso = OkrService.Map(o).Progresso,
                }).ToList(),
                Avaliacoes = criteria.Select(c =>
                {
                    var nota = project.ProjectEvaluations.FirstOrDefault(e => e.PriorityCriteriaId == c.Id);
                    return new NotaCriterioDto
                    {
                        CriterioId = c.Id,
                        CriterioNome = c.Name,
                        Peso = c.ValueWeight,
                        Tipo = c.Type.ToString(),
                        Nota = nota?.Score,
                        AvaliadorNome = nota?.User?.Name,
                        DataAvaliacao = nota?.EvaluatedAt,
                    };
                }).ToList(),
            };
        }

        public async Task<ProjetoDetalheDto> CreateAsync(Guid portfolioId, SalvarProjetoDto dto)
        {
            await _access.EnsureWriteAccessAsync(portfolioId);
            var valid = await ValidateAsync(dto);

            var project = new Project
            {
                PortfolioId = portfolioId,
                Status = ProjectStatus.Rascunho,
                EvaluationStatus = EvaluationStatus.NaoAvaliado,
            };
            Apply(project, valid);

            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            // Figura 27: Configurado -> EmAnalise no primeiro projeto.
            await _portfolioService.ApplyAutomaticEventAsync(portfolioId, PortfolioStateMachine.PrimeiroProjeto);

            return await GetDetailAsync(project.Id);
        }

        public async Task<ProjetoDetalheDto> UpdateAsync(Guid id, SalvarProjetoDto dto)
        {
            await _access.EnsureProjectAccessAsync(id, write: true);
            var project = await _db.Projects.SingleAsync(p => p.Id == id);

            // Cancelado e Arquivado são estados finais: nenhuma edição de dados é permitida (RN22).
            if (ProjectStateMachine.EhFinal(project.Status))
            {
                throw new BusinessRuleException(409, Messages.RN22_Transicao(project.Status, "Editar"));
            }

            var valid = await ValidateAsync(dto);
            var priorityChanged = project.Priority != valid.Priority;
            Apply(project, valid);
            project.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (priorityChanged)
            {
                // a prioridade é critério de desempate do ranking (F2)
                await _prioritization.RecalculateIfNeededAsync(project.PortfolioId);
            }

            return await GetDetailAsync(id);
        }

        public async Task<ProjetoDetalheDto> ChangeStatusAsync(Guid id, string acao)
        {
            await _access.EnsureProjectAccessAsync(id, write: true);
            var project = await _db.Projects.SingleAsync(p => p.Id == id);

            project.Status = ProjectStateMachine.Aplicar(project.Status, NormalizeAction(acao));
            if (project.Status == ProjectStatus.Concluido)
            {
                project.CompletedAt = DateTime.UtcNow;
            }

            project.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // F3: mudança de status do projeto recalcula o ranking (Cancelado/Arquivado saem dele).
            await _prioritization.RecalculateIfNeededAsync(project.PortfolioId);

            return await GetDetailAsync(id);
        }

        private static string NormalizeAction(string? acao)
        {
            var value = acao?.Trim() ?? string.Empty;
            return value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
        }

        private record ValidProject(
            string Name, string? Description, Guid OwnerId, DateOnly Start, DateOnly End, decimal Budget,
            StrategicCategory Category, Priority Priority, string? JiraKey);

        private async Task<ValidProject> ValidateAsync(SalvarProjetoDto dto)
        {
            var errors = new Dictionary<string, string[]>();
            var nome = dto.Nome?.Trim() ?? string.Empty;
            if (nome.Length == 0) errors["nome"] = new[] { "O nome é obrigatório." };
            if (dto.ResponsavelId is null || dto.ResponsavelId == Guid.Empty) errors["responsavelId"] = new[] { "O responsável é obrigatório." };
            if (dto.DataInicio is null) errors["dataInicio"] = new[] { "A data de início é obrigatória." };
            if (dto.DataFim is null) errors["dataFim"] = new[] { "A data de término é obrigatória." };
            if (dto.OrcamentoAprovado is null) errors["orcamentoAprovado"] = new[] { "O orçamento aprovado é obrigatório." };
            if (string.IsNullOrWhiteSpace(dto.CategoriaEstrategica)) errors["categoriaEstrategica"] = new[] { "A categoria estratégica é obrigatória." };

            if (errors.Count > 0)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios, errors);
            }

            if (dto.DataFim < dto.DataInicio)
            {
                throw new BusinessRuleException(400, Messages.RN12_DataFim);
            }

            if (dto.OrcamentoAprovado < 0)
            {
                throw new BusinessRuleException(400, "O orçamento aprovado deve ser maior ou igual a 0.");
            }

            if (!Enum.TryParse<StrategicCategory>(dto.CategoriaEstrategica, true, out var category) || !Enum.IsDefined(category))
            {
                throw new BusinessRuleException(400, "Categoria estratégica inválida. Use Run, Grow ou Transform.");
            }

            var priority = Priority.Media;
            if (!string.IsNullOrWhiteSpace(dto.Prioridade) && (!Enum.TryParse(dto.Prioridade, true, out priority) || !Enum.IsDefined(priority)))
            {
                throw new BusinessRuleException(400, "Prioridade inválida.");
            }

            if (!await _db.Users.AnyAsync(u => u.Id == dto.ResponsavelId && u.IsActive))
            {
                throw Messages.NotFound("Responsável não encontrado.");
            }

            var jira = string.IsNullOrWhiteSpace(dto.JiraProjectKey) ? null : dto.JiraProjectKey.Trim().ToUpperInvariant();
            return new ValidProject(
                nome.Length > 150 ? nome[..150] : nome,
                string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim(),
                dto.ResponsavelId!.Value,
                dto.DataInicio!.Value,
                dto.DataFim!.Value,
                Math.Round(dto.OrcamentoAprovado!.Value, 2),
                category,
                priority,
                jira is { Length: > 50 } ? jira[..50] : jira);
        }

        private static void Apply(Project project, ValidProject v)
        {
            project.Name = v.Name;
            project.Description = v.Description;
            project.OwnerId = v.OwnerId;
            project.StartDate = v.Start;
            project.EndDate = v.End;
            project.ApprovedBudget = v.Budget;
            project.StrategicCategory = v.Category;
            project.Priority = v.Priority;
            project.JiraProjectKey = v.JiraKey;
        }
    }
}
