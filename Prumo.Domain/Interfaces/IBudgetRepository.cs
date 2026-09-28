namespace Prumo.Domain.Interfaces
{
    using Prumo.Domain.Entities;

    public interface IBudgetRepository : IRepository<Budget>
    {
        new Task<Budget?> GetByIdAsync(Guid id);
        Task<Budget?> GetByProjectIdAsync(Guid projectId);
        Task<Budget?> GetByIdWithExpensesAsync(Guid id);
    }
}
