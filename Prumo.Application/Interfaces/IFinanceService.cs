using Prumo.Application.DTOs.Finance;

namespace Prumo.Application.Interfaces
{
    // Financeiro do projeto: lançamentos (RF23).
    public interface IFinanceService
    {
        Task<IEnumerable<LancamentoDto>> ListExpensesAsync(Guid projectId);
        Task<LancamentoDto> AddExpenseAsync(Guid projectId, SalvarLancamentoDto dto);
        Task DeleteExpenseAsync(Guid expenseId);
    }
}
