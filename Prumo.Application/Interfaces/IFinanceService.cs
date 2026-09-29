using Prumo.Application.DTOs.Finance;

namespace Prumo.Application.Interfaces
{
    // Financeiro do projeto: lançamentos (RF23).
    public interface IFinanceService
    {
        Task<IEnumerable<LancamentoDto>> ListExpensesAsync(Guid projectId);
        Task<LancamentoDto> AddExpenseAsync(Guid projectId, SalvarLancamentoDto dto);
        Task DeleteExpenseAsync(Guid expenseId);

        // Business case e retornos (RF25).
        Task<BusinessCaseDto> GetBusinessCaseAsync(Guid projectId);
        Task<BusinessCaseDto> SaveBusinessCaseAsync(Guid projectId, SalvarBusinessCaseDto dto);
        Task<IEnumerable<RetornoDto>> ListReturnsAsync(Guid projectId);
        Task<RetornoDto> AddReturnAsync(Guid projectId, SalvarRetornoDto dto);
    }
}
