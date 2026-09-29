using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Criteria;
using Prumo.Application.Interfaces;
using Prumo.Application.StateMachines;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // Critérios de priorização (RF07–RF09, UC4–UC6, D06).
    public class PriorityCriteriaService : IPriorityCriteriaService
    {
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;
        private readonly IPortfolioService _portfolioService;
        private readonly IPrioritizationService _prioritization;
        private readonly ICurrentUserService _currentUser;

        public PriorityCriteriaService(
            IAppDbContext db,
            IPortfolioAccessService access,
            IPortfolioService portfolioService,
            IPrioritizationService prioritization,
            ICurrentUserService currentUser)
        {
            _db = db;
            _access = access;
            _portfolioService = portfolioService;
            _prioritization = prioritization;
            _currentUser = currentUser;
        }

        /// <summary>GET /portfolios/{id}/criterios — exige ser membro do portfólio.</summary>
        public async Task<IEnumerable<CriterioDto>> ListByPortfolioAsync(Guid portfolioId)
        {
            await _access.EnsureAccessAsync(portfolioId);
            return await Project(_db.PriorityCriterias.AsNoTracking().Where(c => c.PortfolioId == portfolioId))
                .OrderBy(c => c.Nome)
                .ToListAsync();
        }

        public async Task<CriterioDto> CreateAsync(Guid portfolioId, SalvarCriterioDto dto)
        {
            await _access.EnsureWriteAccessAsync(portfolioId);
            var (nome, peso, tipo) = Validate(dto);
            await EnsureUniqueNameAsync(portfolioId, nome, null);

            var criteria = new PriorityCriteria
            {
                PortfolioId = portfolioId,
                Name = nome,
                Description = Clean(dto.Descricao),
                ValueWeight = peso,
                Type = tipo,
                UserId = _currentUser.RequireUserId(),
            };
            _db.PriorityCriterias.Add(criteria);

            // 3.4.3: critério novo -> projetos Priorizado ou Aprovado passam para Reavaliado.
            var afetados = await _db.Projects
                .Where(p => p.PortfolioId == portfolioId
                            && (p.EvaluationStatus == EvaluationStatus.Priorizado || p.EvaluationStatus == EvaluationStatus.Aprovado))
                .ToListAsync();
            foreach (var project in afetados)
            {
                project.EvaluationStatus = EvaluationStateMachine.Aplicar(project.EvaluationStatus, EvaluationStateMachine.CriterioNovo);
            }

            await _db.SaveChangesAsync();

            // Figura 27: Criado -> Configurado no primeiro critério; Monitoramento -> Reavaliacao.
            await _portfolioService.ApplyAutomaticEventAsync(portfolioId, PortfolioStateMachine.PrimeiroCriterio);
            await _portfolioService.ApplyAutomaticEventAsync(portfolioId, PortfolioStateMachine.AlterarCriterio);
            await _prioritization.RecalculateIfNeededAsync(portfolioId);

            return await GetAsync(criteria.Id);
        }

        public async Task<CriterioDto> UpdateAsync(Guid id, SalvarCriterioDto dto)
        {
            var criteria = await FindAsync(id);
            await _access.EnsureWriteAccessAsync(criteria.PortfolioId);
            var (nome, peso, tipo) = Validate(dto);
            await EnsureUniqueNameAsync(criteria.PortfolioId, nome, id);

            var pesoOuTipoMudou = criteria.ValueWeight != peso || criteria.Type != tipo;
            criteria.Name = nome;
            criteria.Description = Clean(dto.Descricao);
            criteria.ValueWeight = peso;
            criteria.Type = tipo;
            criteria.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _portfolioService.ApplyAutomaticEventAsync(criteria.PortfolioId, PortfolioStateMachine.AlterarCriterio);
            if (pesoOuTipoMudou)
            {
                // RF21 / F3: alterar peso ou tipo recalcula o ranking automaticamente.
                await _prioritization.RecalculateIfNeededAsync(criteria.PortfolioId);
            }

            return await GetAsync(id);
        }

        public async Task DeleteAsync(Guid id)
        {
            var criteria = await FindAsync(id);
            await _access.EnsureWriteAccessAsync(criteria.PortfolioId);

            if (await _db.ProjectEvaluations.AnyAsync(e => e.PriorityCriteriaId == id))
            {
                throw new BusinessRuleException(409, Messages.RN11_CriterioComAvaliacoes);
            }

            _db.PriorityCriterias.Remove(criteria);
            await _db.SaveChangesAsync();

            await _portfolioService.ApplyAutomaticEventAsync(criteria.PortfolioId, PortfolioStateMachine.AlterarCriterio);
            await _prioritization.RecalculateIfNeededAsync(criteria.PortfolioId);
        }

        private async Task<PriorityCriteria> FindAsync(Guid id) =>
            await _db.PriorityCriterias.SingleOrDefaultAsync(c => c.Id == id)
            ?? throw Messages.NotFound(Messages.RN10_CriterioNaoEncontrado);

        private async Task<CriterioDto> GetAsync(Guid id) =>
            await Project(_db.PriorityCriterias.AsNoTracking().Where(c => c.Id == id)).SingleAsync();

        private static (string Nome, decimal Peso, CriteriaType Tipo) Validate(SalvarCriterioDto dto)
        {
            var nome = dto.Nome?.Trim() ?? string.Empty;
            if (nome.Length == 0)
            {
                throw new BusinessRuleException(400, Messages.RN08_NomeCriterio,
                    new Dictionary<string, string[]> { ["nome"] = new[] { Messages.RN08_NomeCriterio } });
            }

            if (dto.Peso is null)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios,
                    new Dictionary<string, string[]> { ["peso"] = new[] { "O peso é obrigatório." } });
            }

            if (dto.Peso <= 0 || dto.Peso > 10)
            {
                throw new BusinessRuleException(400, Messages.RN07_PesoInvalido);
            }

            var tipo = CriteriaType.Beneficio;
            if (!string.IsNullOrWhiteSpace(dto.Tipo) && (!Enum.TryParse(dto.Tipo, true, out tipo) || !Enum.IsDefined(tipo)))
            {
                throw new BusinessRuleException(400, "Tipo de critério inválido. Use Beneficio ou Custo.");
            }

            return (nome.Length > 100 ? nome[..100] : nome, Math.Round(dto.Peso.Value, 2), tipo);
        }

        private async Task EnsureUniqueNameAsync(Guid portfolioId, string nome, Guid? currentId)
        {
            var lowered = nome.ToLower();
            var exists = await _db.PriorityCriterias.AnyAsync(c =>
                c.PortfolioId == portfolioId && c.Id != currentId && c.Name.ToLower() == lowered);
            if (exists)
            {
                throw new BusinessRuleException(409, Messages.RN09_CriterioRepetido);
            }
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private IQueryable<CriterioDto> Project(IQueryable<PriorityCriteria> query) =>
            query.Select(c => new CriterioDto
            {
                Id = c.Id,
                PortfolioId = c.PortfolioId,
                Nome = c.Name,
                Descricao = c.Description,
                Peso = c.ValueWeight,
                Tipo = c.Type.ToString(),
                QuantidadeAvaliacoes = _db.ProjectEvaluations.Count(e => e.PriorityCriteriaId == c.Id),
            });
    }
}
