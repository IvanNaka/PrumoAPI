using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Finance;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // Lançamentos financeiros (RF23, RN28).
    public class FinanceService : IFinanceService
    {
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;

        public FinanceService(IAppDbContext db, IPortfolioAccessService access)
        {
            _db = db;
            _access = access;
        }

        public async Task<IEnumerable<LancamentoDto>> ListExpensesAsync(Guid projectId)
        {
            await _access.EnsureProjectAccessAsync(projectId);
            var expenses = await _db.BudgetExpenses.AsNoTracking()
                .Where(e => e.ProjectId == projectId)
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedDate)
                .ToListAsync();
            return expenses.Select(Map);
        }

        public async Task<LancamentoDto> AddExpenseAsync(Guid projectId, SalvarLancamentoDto dto)
        {
            await _access.EnsureProjectAccessAsync(projectId, write: true);

            if (string.IsNullOrWhiteSpace(dto.Descricao) || dto.Valor is null || dto.DataLancamento is null || string.IsNullOrWhiteSpace(dto.Tipo))
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios);
            }

            // RN28: valor positivo e data que não esteja no futuro.
            if (dto.Valor <= 0 || dto.DataLancamento > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new BusinessRuleException(400, Messages.RN28_LancamentoInvalido);
            }

            if (!Enum.TryParse<BudgetExpenseCategory>(dto.Tipo, true, out var tipo) || !Enum.IsDefined(tipo))
            {
                throw new BusinessRuleException(400, "Tipo de lançamento inválido. Use Custo ou Despesa.");
            }

            var descricao = dto.Descricao.Trim();
            var expense = new BudgetExpense
            {
                ProjectId = projectId,
                Description = descricao.Length > 300 ? descricao[..300] : descricao,
                Amount = Math.Round(dto.Valor.Value, 2),
                Category = tipo,
                Date = dto.DataLancamento.Value,
            };
            _db.BudgetExpenses.Add(expense);
            await _db.SaveChangesAsync();
            return Map(expense);
        }

        public async Task DeleteExpenseAsync(Guid expenseId)
        {
            var expense = await _db.BudgetExpenses.SingleOrDefaultAsync(e => e.Id == expenseId)
                ?? throw Messages.NotFound("Lançamento não encontrado.");
            await _access.EnsureProjectAccessAsync(expense.ProjectId, write: true);
            _db.BudgetExpenses.Remove(expense);
            await _db.SaveChangesAsync();
        }

        public async Task<BusinessCaseDto> GetBusinessCaseAsync(Guid projectId)
        {
            await _access.EnsureProjectAccessAsync(projectId);
            var bc = await _db.BusinessCases.AsNoTracking().Include(b => b.Flows).SingleOrDefaultAsync(b => b.ProjectId == projectId);
            return MapBusinessCase(projectId, bc);
        }

        public async Task<BusinessCaseDto> SaveBusinessCaseAsync(Guid projectId, SalvarBusinessCaseDto dto)
        {
            await _access.EnsureProjectAccessAsync(projectId, write: true);

            if (dto.InvestimentoInicial is null || dto.TaxaDescontoAnual is null)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios);
            }

            if (dto.InvestimentoInicial < 0)
            {
                throw new BusinessRuleException(400, "O investimento inicial deve ser maior ou igual a 0.");
            }

            if (dto.TaxaDescontoAnual is < 0 or > 100)
            {
                throw new BusinessRuleException(400, "A taxa de desconto anual deve estar entre 0 e 100%.");
            }

            var fluxos = dto.FluxosPrevistos ?? new List<FluxoCaixaDto>();
            if (fluxos.Any(f => f.Mes < 1))
            {
                throw new BusinessRuleException(400, "O mês do fluxo de caixa deve ser maior ou igual a 1.");
            }

            if (fluxos.GroupBy(f => f.Mes).Any(g => g.Count() > 1))
            {
                throw new BusinessRuleException(400, "Cada mês pode ter apenas um fluxo de caixa previsto.");
            }

            var bc = await _db.BusinessCases.Include(b => b.Flows).SingleOrDefaultAsync(b => b.ProjectId == projectId);
            if (bc == null)
            {
                bc = new BusinessCase { ProjectId = projectId };
                _db.BusinessCases.Add(bc);
            }

            bc.InitialInvestment = Math.Round(dto.InvestimentoInicial.Value, 2);
            bc.AnnualDiscountRate = Math.Round(dto.TaxaDescontoAnual.Value, 4);
            bc.UpdatedDate = DateTime.UtcNow;

            // PUT substitui a lista de fluxos inteira.
            foreach (var flow in bc.Flows.ToList())
            {
                _db.CashFlowForecasts.Remove(flow);
            }
            bc.Flows.Clear();
            foreach (var fluxo in fluxos.OrderBy(f => f.Mes))
            {
                // Adicionado pelo DbSet: com o Id já preenchido, a navegação faria o EF tratá-lo como existente.
                _db.CashFlowForecasts.Add(new CashFlowForecast { BusinessCaseId = bc.Id, Month = fluxo.Mes, Value = Math.Round(fluxo.Valor, 2) });
            }

            await _db.SaveChangesAsync();
            return await GetBusinessCaseAsync(projectId);
        }

        public async Task<IEnumerable<RetornoDto>> ListReturnsAsync(Guid projectId)
        {
            await _access.EnsureProjectAccessAsync(projectId);
            var returns = await _db.RealizedReturns.AsNoTracking()
                .Where(r => r.ProjectId == projectId)
                .OrderBy(r => r.Date)
                .ToListAsync();
            return returns.Select(MapReturn);
        }

        public async Task<RetornoDto> AddReturnAsync(Guid projectId, SalvarRetornoDto dto)
        {
            await _access.EnsureProjectAccessAsync(projectId, write: true);
            if (dto.Data is null || dto.Valor is null)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios);
            }

            var descricao = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim();
            var entity = new RealizedReturn
            {
                ProjectId = projectId,
                Date = dto.Data.Value,
                Value = Math.Round(dto.Valor.Value, 2),
                Description = descricao is { Length: > 300 } ? descricao[..300] : descricao,
            };
            _db.RealizedReturns.Add(entity);
            await _db.SaveChangesAsync();
            return MapReturn(entity);
        }

        private static BusinessCaseDto MapBusinessCase(Guid projectId, BusinessCase? bc) => new()
        {
            Id = bc?.Id,
            ProjetoId = projectId,
            Cadastrado = bc != null,
            InvestimentoInicial = bc?.InitialInvestment ?? 0,
            TaxaDescontoAnual = bc?.AnnualDiscountRate ?? 0,
            FluxosPrevistos = bc?.Flows.OrderBy(f => f.Month).Select(f => new FluxoCaixaDto { Mes = f.Month, Valor = f.Value }).ToList()
                              ?? new List<FluxoCaixaDto>(),
        };

        private static RetornoDto MapReturn(RealizedReturn r) => new()
        {
            Id = r.Id,
            ProjetoId = r.ProjectId,
            Data = r.Date,
            Valor = r.Value,
            Descricao = r.Description,
        };

        private static LancamentoDto Map(BudgetExpense e) => new()
        {
            Id = e.Id,
            ProjetoId = e.ProjectId,
            Descricao = e.Description,
            Valor = e.Amount,
            Tipo = e.Category.ToString(),
            DataLancamento = e.Date,
        };
    }
}
