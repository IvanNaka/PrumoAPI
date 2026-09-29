using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Portfolio;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // RF04–RF06, UC2, UC3, Figura 27.
    [ApiController]
    [Authorize]
    [Route("api/portfolios")]
    public class PortfoliosController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;

        public PortfoliosController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        /// <summary>Portfólios visíveis para o usuário (D11). Lista vazia = RN05 no front-end.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PortfolioDto>>> GetAll()
        {
            return Ok(await _portfolioService.GetVisibleAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PortfolioDto>> GetById(Guid id)
        {
            var portfolio = await _portfolioService.GetByIdAsync(id);
            return portfolio == null ? NotFound() : Ok(portfolio);
        }

        [HttpPost]
        [Authorize(Policy = Policies.EditarPortfolio)]
        public async Task<ActionResult<PortfolioDto>> Create([FromBody] CreatePortfolioDto dto)
        {
            var created = await _portfolioService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Policies.EditarPortfolio)]
        public async Task<ActionResult<PortfolioDto>> Update(Guid id, [FromBody] CreatePortfolioDto dto)
        {
            return Ok(await _portfolioService.UpdateAsync(id, dto));
        }

        /// <summary>Aprovar portfólio, reavaliar estratégia ou encerrar (3.4.2).</summary>
        [HttpPost("{id:guid}/acoes/{acao}")]
        [Authorize(Policy = Policies.GovernarPortfolio)]
        public async Task<ActionResult<PortfolioDto>> Action(Guid id, string acao)
        {
            return Ok(await _portfolioService.ApplyActionAsync(id, acao));
        }

        [HttpGet("{id:guid}/membros")]
        public async Task<ActionResult<IEnumerable<PortfolioMemberDto>>> GetMembers(Guid id)
        {
            return Ok(await _portfolioService.GetMembersAsync(id));
        }

        /// <summary>Somente o responsável do portfólio ou o Administrador.</summary>
        [HttpPost("{id:guid}/membros")]
        public async Task<ActionResult<IEnumerable<PortfolioMemberDto>>> AddMember(Guid id, [FromBody] AddPortfolioMemberDto dto)
        {
            return Ok(await _portfolioService.AddMemberAsync(id, dto.UsuarioId));
        }

        [HttpDelete("{id:guid}/membros/{usuarioId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid usuarioId)
        {
            await _portfolioService.RemoveMemberAsync(id, usuarioId);
            return NoContent();
        }
    }
}
