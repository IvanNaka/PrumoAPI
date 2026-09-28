using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.TeamCapacity;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamCapacitiesController : ControllerBase
    {
        private readonly ITeamCapacityService _teamCapacityService;
        private readonly ITeamService _teamService;

        public TeamCapacitiesController(ITeamCapacityService teamCapacityService, ITeamService teamService)
        {
            _teamCapacityService = teamCapacityService;
            _teamService = teamService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TeamCapacityEntryDto>> GetById(Guid id)
        {
            var entry = await _teamCapacityService.GetByIdAsync(id);
            if (entry == null)
            {
                return NotFound();
            }

            return Ok(entry);
        }

        [HttpGet("team/{teamId:guid}")]
        public async Task<ActionResult<IEnumerable<TeamCapacityEntryDto>>> GetByTeamId(Guid teamId)
        {
            var team = await _teamService.GetByIdAsync(teamId);
            if (team == null)
            {
                return NotFound();
            }

            var entries = await _teamCapacityService.GetByTeamIdAsync(teamId);
            return Ok(entries);
        }

        [HttpPost]
        public async Task<ActionResult<TeamCapacityEntryDto>> Create([FromBody] CreateTeamCapacityDto createDto)
        {
            if (createDto == null)
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                var (entry, created) = await _teamCapacityService.UpsertAsync(createDto);
                return created
                    ? CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry)
                    : Ok(entry);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamCapacityDto updateDto)
        {
            if (updateDto == null || id != updateDto.Id)
            {
                return BadRequest("ID mismatch or invalid payload.");
            }

            try
            {
                await _teamCapacityService.UpdateAsync(updateDto);
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

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _teamCapacityService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            await _teamCapacityService.DeleteAsync(id);
            return NoContent();
        }
    }
}
