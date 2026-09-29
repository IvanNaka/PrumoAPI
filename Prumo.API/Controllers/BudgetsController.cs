using Microsoft.AspNetCore.Authorization;
using Prumo.API.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Budget;
using Prumo.Application.DTOs.BudgetExpense;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Authorize(Policy = Policies.VerFinanceiro)]
    [Route("api/[controller]")]
    public class BudgetsController : ControllerBase
    {
        private readonly IBudgetService _budgetService;

        public BudgetsController(IBudgetService budgetService)
        {
            _budgetService = budgetService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BudgetDto>> GetById(Guid id)
        {
            var budget = await _budgetService.GetByIdAsync(id);
            if (budget == null)
            {
                return NotFound();
            }

            return Ok(budget);
        }

        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<BudgetDto>> GetByProjectId(Guid projectId)
        {
            var budget = await _budgetService.GetByProjectIdAsync(projectId);
            if (budget == null)
            {
                return NotFound();
            }

            return Ok(budget);
        }

        [Authorize(Policy = Policies.EditarFinanceiro)]
        [HttpPost]
        public async Task<ActionResult<BudgetDto>> Create([FromBody] CreateBudgetDto createDto)
        {
            if (createDto == null)
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                var created = await _budgetService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [Authorize(Policy = Policies.EditarFinanceiro)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetDto updateDto)
        {
            if (updateDto == null)
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                await _budgetService.UpdateAsync(id, updateDto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{budgetId:guid}/expenses")]
        public async Task<ActionResult<IEnumerable<BudgetExpenseDto>>> GetExpenses(Guid budgetId)
        {
            var budget = await _budgetService.GetByIdAsync(budgetId);
            if (budget == null)
            {
                return NotFound();
            }

            var expenses = await _budgetService.GetExpensesAsync(budgetId);
            return Ok(expenses);
        }

        [Authorize(Policy = Policies.EditarFinanceiro)]
        [HttpPost("{budgetId:guid}/expenses")]
        public async Task<ActionResult<BudgetExpenseDto>> CreateExpense(Guid budgetId, [FromBody] CreateBudgetExpenseDto createDto)
        {
            if (createDto == null)
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                var created = await _budgetService.AddExpenseAsync(budgetId, createDto);
                return CreatedAtAction(nameof(GetExpenses), new { budgetId }, created);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Policy = Policies.EditarFinanceiro)]
        [HttpDelete("expenses/{expenseId:guid}")]
        public async Task<IActionResult> DeleteExpense(Guid expenseId)
        {
            var existingExpense = await _budgetService.GetExpenseByIdAsync(expenseId);
            if (existingExpense == null)
            {
                return NotFound();
            }

            await _budgetService.DeleteExpenseAsync(expenseId);
            return NoContent();
        }

        [HttpGet("{budgetId:guid}/metrics")]
        public async Task<ActionResult<BudgetMetricsDto>> GetMetrics(Guid budgetId)
        {
            var metrics = await _budgetService.GetMetricsAsync(budgetId);
            if (metrics == null)
            {
                return NotFound();
            }

            return Ok(metrics);
        }
    }
}
