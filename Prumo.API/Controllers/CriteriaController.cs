using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Criteria;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Critérios de priorização (RF07–RF09, UC4–UC6).
    [ApiController]
    [Authorize]
    [Route("api")]
    public class CriteriaController : ControllerBase
    {
        private readonly IPriorityCriteriaService _service;

        public CriteriaController(IPriorityCriteriaService service)
        {
            _service = service;
        }

        [HttpGet("portfolios/{portfolioId:guid}/criterios")]
        public async Task<ActionResult<IEnumerable<CriterioDto>>> List(Guid portfolioId)
        {
            return Ok(await _service.ListByPortfolioAsync(portfolioId));
        }

        [HttpPost("portfolios/{portfolioId:guid}/criterios")]
        [Authorize(Policy = Policies.EditarCriterios)]
        public async Task<ActionResult<CriterioDto>> Create(Guid portfolioId, [FromBody] SalvarCriterioDto dto)
        {
            var created = await _service.CreateAsync(portfolioId, dto);
            return Created($"/api/criterios/{created.Id}", created);
        }

        [HttpPut("portfolios/{portfolioId:guid}/criterios/pesos")]
        [Authorize(Policy = Policies.EditarCriterios)]
        public async Task<ActionResult<IEnumerable<CriterioDto>>> UpdateWeights(Guid portfolioId, [FromBody] List<PesoCriterioDto> pesos)
        {
            return Ok(await _service.UpdateWeightsAsync(portfolioId, pesos ?? new List<PesoCriterioDto>()));
        }

        [HttpPut("criterios/{id:guid}")]
        [Authorize(Policy = Policies.EditarCriterios)]
        public async Task<ActionResult<CriterioDto>> Update(Guid id, [FromBody] SalvarCriterioDto dto)
        {
            return Ok(await _service.UpdateAsync(id, dto));
        }

        [HttpDelete("criterios/{id:guid}")]
        [Authorize(Policy = Policies.EditarCriterios)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
