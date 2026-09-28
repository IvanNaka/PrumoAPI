using Prumo.Application.DTOs.Budget;
using Prumo.Application.DTOs.BudgetExpense;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Prumo.Application.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly IBudgetRepository _budgetRepository;
        private readonly IBudgetExpenseRepository _budgetExpenseRepository;
        private readonly IProjectRepository _projectRepository;

        public BudgetService(
            IBudgetRepository budgetRepository,
            IBudgetExpenseRepository budgetExpenseRepository,
            IProjectRepository projectRepository)
        {
            _budgetRepository = budgetRepository;
            _budgetExpenseRepository = budgetExpenseRepository;
            _projectRepository = projectRepository;
        }

        public async Task<BudgetDto> GetByIdAsync(Guid id)
        {
            var budget = await _budgetRepository.GetByIdAsync(id);
            return budget == null ? null : MapToDto(budget);
        }

        public async Task<BudgetDto> GetByProjectIdAsync(Guid projectId)
        {
            var budget = await _budgetRepository.GetByProjectIdAsync(projectId);
            return budget == null ? null : MapToDto(budget);
        }

        public async Task<BudgetDto> CreateAsync(CreateBudgetDto dto)
        {
            if (dto.TotalAmount <= 0)
            {
                throw new ArgumentException("totalAmount deve ser maior que zero.");
            }

            if (dto.StartDate >= dto.EndDate)
            {
                throw new ArgumentException("endDate deve ser posterior a startDate.");
            }

            var project = await _projectRepository.GetByIdAsync(dto.ProjectId);
            if (project == null)
            {
                throw new ArgumentException("projectId inexistente.");
            }

            var existingBudget = await _budgetRepository.GetByProjectIdAsync(dto.ProjectId);
            if (existingBudget != null)
            {
                throw new InvalidOperationException("O projeto já possui um orçamento cadastrado.");
            }

            var budget = new Budget
            {
                ProjectId = dto.ProjectId,
                TotalAmount = dto.TotalAmount,
                Currency = dto.Currency,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DiscountRateMonthly = dto.DiscountRateMonthly,
                ExpectedReturn = dto.ExpectedReturn,
            };

            await _budgetRepository.AddAsync(budget);
            budget.Project = project;

            return MapToDto(budget);
        }

        public async Task UpdateAsync(Guid id, UpdateBudgetDto dto)
        {
            var existing = await _budgetRepository.GetByIdAsync(id);
            if (existing == null)
            {
                throw new KeyNotFoundException("Orçamento não encontrado.");
            }

            if (dto.TotalAmount <= 0)
            {
                throw new ArgumentException("totalAmount deve ser maior que zero.");
            }

            if (dto.StartDate >= dto.EndDate)
            {
                throw new ArgumentException("endDate deve ser posterior a startDate.");
            }

            existing.TotalAmount = dto.TotalAmount;
            existing.Currency = dto.Currency;
            existing.StartDate = dto.StartDate;
            existing.EndDate = dto.EndDate;
            existing.DiscountRateMonthly = dto.DiscountRateMonthly;
            existing.ExpectedReturn = dto.ExpectedReturn;
            existing.UpdatedDate = DateTime.UtcNow;

            await _budgetRepository.UpdateAsync(existing);
        }

        public async Task<IEnumerable<BudgetExpenseDto>> GetExpensesAsync(Guid budgetId)
        {
            var expenses = await _budgetExpenseRepository.GetByBudgetIdAsync(budgetId);
            return expenses.Select(MapToDto);
        }

        public async Task<BudgetExpenseDto> AddExpenseAsync(Guid budgetId, CreateBudgetExpenseDto dto)
        {
            var budget = await _budgetRepository.GetByIdAsync(budgetId);
            if (budget == null)
            {
                throw new KeyNotFoundException("Orçamento não encontrado.");
            }

            if (dto.Amount <= 0)
            {
                throw new ArgumentException("amount deve ser maior que zero.");
            }

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                throw new ArgumentException("description é obrigatória.");
            }

            var expense = new BudgetExpense
            {
                BudgetId = budgetId,
                Description = dto.Description,
                Category = dto.Category,
                Amount = dto.Amount,
                Date = dto.Date,
            };

            await _budgetExpenseRepository.AddAsync(expense);

            return MapToDto(expense);
        }

        public async Task<BudgetExpenseDto> GetExpenseByIdAsync(Guid expenseId)
        {
            var expense = await _budgetExpenseRepository.GetByIdAsync(expenseId);
            return expense == null ? null : MapToDto(expense);
        }

        public async Task DeleteExpenseAsync(Guid expenseId)
        {
            await _budgetExpenseRepository.DeleteAsync(expenseId);
        }

        public async Task<BudgetMetricsDto> GetMetricsAsync(Guid budgetId)
        {
            var budget = await _budgetRepository.GetByIdWithExpensesAsync(budgetId);
            if (budget == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;

            var totalBudget = budget.TotalAmount;
            var totalSpent = budget.Expenses.Sum(e => e.Amount);
            var remainingBudget = totalBudget - totalSpent;
            var percentSpent = totalBudget > 0
                ? (int)Math.Round(totalSpent / totalBudget * 100, MidpointRounding.AwayFromZero)
                : 0;

            var monthsElapsed = MonthsBetween(budget.StartDate, now);
            var monthsRemaining = MonthsBetween(now, budget.EndDate);

            // Ao menos 1 mês para evitar divisão por zero ao calcular o Burn Rate
            // logo no início do período orçamentário.
            var burnRateMonthly = totalSpent / Math.Max(monthsElapsed, 1);
            var burnRatePercent = totalBudget > 0
                ? (int)Math.Round(burnRateMonthly / totalBudget * 100, MidpointRounding.AwayFromZero)
                : 0;

            var netPresentValue = CalculateNetPresentValue(
                totalSpent, budget.ExpectedReturn, budget.DiscountRateMonthly, monthsRemaining);

            DateTime? projectedDepletionDate;
            bool isOverBudgetRisk;

            if (remainingBudget <= 0)
            {
                projectedDepletionDate = now;
                isOverBudgetRisk = true;
            }
            else if (burnRateMonthly <= 0)
            {
                projectedDepletionDate = null;
                isOverBudgetRisk = false;
            }
            else
            {
                var monthsUntilDepletion = (double)(remainingBudget / burnRateMonthly);
                projectedDepletionDate = now.AddMonths((int)Math.Ceiling(monthsUntilDepletion));
                isOverBudgetRisk = projectedDepletionDate.Value < budget.EndDate;
            }

            return new BudgetMetricsDto
            {
                BudgetId = budget.Id,
                TotalBudget = totalBudget,
                TotalSpent = totalSpent,
                RemainingBudget = remainingBudget,
                PercentSpent = percentSpent,
                NetPresentValue = netPresentValue,
                BurnRateMonthly = burnRateMonthly,
                BurnRatePercent = burnRatePercent,
                MonthsElapsed = monthsElapsed,
                MonthsRemaining = monthsRemaining,
                ProjectedDepletionDate = projectedDepletionDate,
                IsOverBudgetRisk = isOverBudgetRisk,
            };
        }

        // VPL = -totalSpent + Σ (expectedReturn / monthsRemaining) / (1 + discountRateMonthly/100)^t,
        // para t = 1..monthsRemaining.
        private static decimal CalculateNetPresentValue(
            decimal totalSpent, decimal expectedReturn, decimal discountRateMonthly, int monthsRemaining)
        {
            if (monthsRemaining <= 0)
            {
                return -totalSpent;
            }

            var monthlyReturn = expectedReturn / monthsRemaining;
            var monthlyRate = (double)discountRateMonthly / 100.0;

            double presentValueOfReturns = 0;
            for (var t = 1; t <= monthsRemaining; t++)
            {
                presentValueOfReturns += (double)monthlyReturn / Math.Pow(1 + monthlyRate, t);
            }

            return -totalSpent + (decimal)presentValueOfReturns;
        }

        private static int MonthsBetween(DateTime from, DateTime to)
        {
            if (to <= from)
            {
                return 0;
            }

            var months = ((to.Year - from.Year) * 12) + to.Month - from.Month;
            if (to.Day < from.Day)
            {
                months--;
            }

            return Math.Max(0, months);
        }

        private static BudgetDto MapToDto(Budget budget)
        {
            return new BudgetDto
            {
                Id = budget.Id,
                ProjectId = budget.ProjectId,
                ProjectName = budget.Project?.Name,
                TotalAmount = budget.TotalAmount,
                Currency = budget.Currency,
                StartDate = budget.StartDate,
                EndDate = budget.EndDate,
                DiscountRateMonthly = budget.DiscountRateMonthly,
                ExpectedReturn = budget.ExpectedReturn,
                CreatedAt = budget.CreatedDate,
                UpdatedAt = budget.UpdatedDate,
            };
        }

        private static BudgetExpenseDto MapToDto(BudgetExpense expense)
        {
            return new BudgetExpenseDto
            {
                Id = expense.Id,
                BudgetId = expense.BudgetId,
                Description = expense.Description,
                Category = expense.Category,
                Amount = expense.Amount,
                Date = expense.Date,
                CreatedAt = expense.CreatedDate,
            };
        }
    }
}
