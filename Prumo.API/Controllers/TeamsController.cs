using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Team;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Equipes, membros e capacidade (RF29–RF32, UC13).
    [ApiController]
    [Authorize]
    [Route("api/equipes")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipeDto>>> GetAll()
        {
            return Ok(await _teamService.GetAllAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<EquipeDto>> Get(Guid id)
        {
            return Ok(await _teamService.GetAsync(id));
        }

        [HttpPost]
        [Authorize(Policy = Policies.EditarEquipes)]
        public async Task<ActionResult<EquipeDto>> Create([FromBody] SalvarEquipeDto dto)
        {
            var created = await _teamService.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Policies.EditarEquipes)]
        public async Task<ActionResult<EquipeDto>> Update(Guid id, [FromBody] SalvarEquipeDto dto)
        {
            return Ok(await _teamService.UpdateAsync(id, dto));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Policies.EditarEquipes)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _teamService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id:guid}/codigo-convite")]
        [Authorize(Policy = Policies.EditarEquipes)]
        public async Task<ActionResult<EquipeDto>> RegenerateInviteCode(Guid id)
        {
            return Ok(await _teamService.RegenerateInviteCodeAsync(id));
        }

        [HttpGet("{id:guid}/membros")]
        public async Task<ActionResult<IEnumerable<MembroEquipeDto>>> GetMembers(Guid id)
        {
            return Ok(await _teamService.GetMembersAsync(id));
        }

        [HttpPost("{id:guid}/membros")]
        [Authorize(Policy = Policies.EditarEquipes)]
        public async Task<ActionResult<MembroEquipeDto>> AddMember(Guid id, [FromBody] SalvarMembroDto dto)
        {
            var created = await _teamService.AddMemberAsync(id, dto);
            return Created($"/api/equipes/{id}/membros/{created.Id}", created);
        }

        [HttpPut("{id:guid}/membros/{membroId:guid}")]
        [Authorize(Policy = Policies.EditarEquipes)]
        public async Task<ActionResult<MembroEquipeDto>> UpdateMember(Guid id, Guid membroId, [FromBody] SalvarMembroDto dto)
        {
            return Ok(await _teamService.UpdateMemberAsync(id, membroId, dto));
        }

        [HttpDelete("{id:guid}/membros/{membroId:guid}")]
        [Authorize(Policy = Policies.EditarEquipes)]
        public async Task<IActionResult> RemoveMember(Guid id, Guid membroId)
        {
            await _teamService.RemoveMemberAsync(id, membroId);
            return NoContent();
        }

        /// <summary>Capacidade, ocupação e utilização da equipe no mês (F9): ?mes=AAAA-MM.</summary>
        [HttpGet("{id:guid}/capacidade")]
        public async Task<ActionResult<CapacityResult>> Capacity(Guid id, [FromQuery] string? mes)
        {
            return Ok(await _teamService.GetCapacityAsync(id, mes));
        }
    }
}
