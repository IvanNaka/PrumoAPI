using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Project;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Projetos (RF10–RF13, UC7, Figura 26).
    [ApiController]
    [Authorize]
    [Route("api")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet("portfolios/{portfolioId:guid}/projetos")]
        public async Task<ActionResult<IEnumerable<ProjetoResumoDto>>> List(Guid portfolioId, [FromQuery] string? status, [FromQuery] string? categoria)
        {
            return Ok(await _projectService.ListByPortfolioAsync(portfolioId, status, categoria));
        }

        [HttpGet("projetos/{id:guid}")]
        public async Task<ActionResult<ProjetoDetalheDto>> GetById(Guid id)
        {
            return Ok(await _projectService.GetDetailAsync(id));
        }

        [HttpPost("portfolios/{portfolioId:guid}/projetos")]
        [Authorize(Policy = Policies.EditarProjetos)]
        public async Task<ActionResult<ProjetoDetalheDto>> Create(Guid portfolioId, [FromBody] SalvarProjetoDto dto)
        {
            var created = await _projectService.CreateAsync(portfolioId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("projetos/{id:guid}")]
        [Authorize(Policy = Policies.EditarProjetos)]
        public async Task<ActionResult<ProjetoDetalheDto>> Update(Guid id, [FromBody] SalvarProjetoDto dto)
        {
            return Ok(await _projectService.UpdateAsync(id, dto));
        }

        /// <summary>Muda o status do projeto: { acao } — uma das ações da Figura 26 (ou Cancelar).</summary>
        [HttpPost("projetos/{id:guid}/status")]
        [Authorize(Policy = Policies.EditarProjetos)]
        public async Task<ActionResult<ProjetoDetalheDto>> ChangeStatus(Guid id, [FromBody] AlterarStatusProjetoDto dto)
        {
            return Ok(await _projectService.ChangeStatusAsync(id, dto?.Acao ?? string.Empty));
        }
    }
}
