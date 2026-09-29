using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Finance;
using Prumo.Application.DTOs.Project;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Financeiro do projeto (RF22–RF25) e indicadores do projeto.
    [ApiController]
    [Authorize]
    [Route("api")]
    public class FinanceController : ControllerBase
    {
        private readonly IFinanceService _finance;
        private readonly IProjectIndicatorsService _indicators;

        public FinanceController(IFinanceService finance, IProjectIndicatorsService indicators)
        {
            _finance = finance;
            _indicators = indicators;
        }

        [HttpGet("projetos/{id:guid}/lancamentos")]
        [Authorize(Policy = Policies.VerFinanceiro)]
        public async Task<ActionResult<IEnumerable<LancamentoDto>>> ListExpenses(Guid id)
        {
            return Ok(await _finance.ListExpensesAsync(id));
        }

        [HttpPost("projetos/{id:guid}/lancamentos")]
        [Authorize(Policy = Policies.EditarFinanceiro)]
        public async Task<ActionResult<LancamentoDto>> AddExpense(Guid id, [FromBody] SalvarLancamentoDto dto)
        {
            var created = await _finance.AddExpenseAsync(id, dto);
            return Created($"/api/lancamentos/{created.Id}", created);
        }

        [HttpDelete("lancamentos/{id:guid}")]
        [Authorize(Policy = Policies.EditarFinanceiro)]
        public async Task<IActionResult> DeleteExpense(Guid id)
        {
            await _finance.DeleteExpenseAsync(id);
            return NoContent();
        }

        /// <summary>F5, F6, F7, F8 e F12 do projeto.</summary>
        [HttpGet("projetos/{id:guid}/indicadores")]
        public async Task<ActionResult<ProjetoIndicadoresDto>> Indicators(Guid id)
        {
            return Ok(await _indicators.GetAsync(id));
        }
    }
}
