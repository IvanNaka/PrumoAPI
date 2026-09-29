using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Dependency;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Dependências entre projetos (RF26–RF28, UC12).
    [ApiController]
    [Authorize]
    [Route("api")]
    public class DependenciesController : ControllerBase
    {
        private readonly IProjectDependencyService _service;

        public DependenciesController(IProjectDependencyService service)
        {
            _service = service;
        }

        /// <summary>Dependências do portfólio, cada uma com emRisco e motivo (F13).</summary>
        [HttpGet("portfolios/{portfolioId:guid}/dependencias")]
        public async Task<ActionResult<IEnumerable<DependenciaDto>>> ListByPortfolio(Guid portfolioId)
        {
            return Ok(await _service.ListByPortfolioAsync(portfolioId));
        }

        [HttpGet("projetos/{projetoId:guid}/dependencias")]
        public async Task<ActionResult<IEnumerable<DependenciaDto>>> ListByProject(Guid projetoId)
        {
            return Ok(await _service.ListByProjectAsync(projetoId));
        }

        [HttpPost("dependencias")]
        [Authorize(Policy = Policies.EditarDependencias)]
        public async Task<ActionResult<DependenciaDto>> Create([FromBody] CriarDependenciaDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return Created($"/api/dependencias/{created.Id}", created);
        }

        [HttpDelete("dependencias/{id:guid}")]
        [Authorize(Policy = Policies.EditarDependencias)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
