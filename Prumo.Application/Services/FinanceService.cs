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
