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

        [HttpGet("projetos/{id:guid}/business-case")]
        [Authorize(Policy = Policies.VerFinanceiro)]
        public async Task<ActionResult<BusinessCaseDto>> GetBusinessCase(Guid id)
        {
            return Ok(await _finance.GetBusinessCaseAsync(id));
        }

        /// <summary>Grava o business case; a lista de fluxos previstos é substituída inteira.</summary>
        [HttpPut("projetos/{id:guid}/business-case")]
        [Authorize(Policy = Policies.EditarFinanceiro)]
        public async Task<ActionResult<BusinessCaseDto>> SaveBusinessCase(Guid id, [FromBody] SalvarBusinessCaseDto dto)
        {
            return Ok(await _finance.SaveBusinessCaseAsync(id, dto));
        }

        [HttpGet("projetos/{id:guid}/retornos")]
        [Authorize(Policy = Policies.VerFinanceiro)]
        public async Task<ActionResult<IEnumerable<RetornoDto>>> ListReturns(Guid id)
        {
            return Ok(await _finance.ListReturnsAsync(id));
        }

        [HttpPost("projetos/{id:guid}/retornos")]
        [Authorize(Policy = Policies.EditarFinanceiro)]
        public async Task<ActionResult<RetornoDto>> AddReturn(Guid id, [FromBody] SalvarRetornoDto dto)
        {
            var created = await _finance.AddReturnAsync(id, dto);
            return Created($"/api/projetos/{id}/retornos", created);
        }

        /// <summary>F5, F6, F7, F8 e F12 do projeto.</summary>
        [HttpGet("projetos/{id:guid}/indicadores")]
        public async Task<ActionResult<ProjetoIndicadoresDto>> Indicators(Guid id, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
        {
            return Ok(await _indicators.GetAsync(id, de, ate));
        }
    }
}
