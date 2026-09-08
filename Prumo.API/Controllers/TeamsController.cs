using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Team;
using Prumo.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Prumo.API.Controllers
{
    // NOTE: [Authorize] temporarily removed - all endpoints are open while role
    // permissions are disabled. Re-add [Authorize] / role checks when re-enabling.
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TeamDto>> GetById(Guid id)
        {
            var team = await _teamService.GetByIdAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            return Ok(team);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeamDto>>> GetAll()
        {
            var teams = await _teamService.GetAllAsync();
            return Ok(teams);
        }

        [HttpGet("portfolio/{portfolioId:guid}")]
        public async Task<ActionResult<IEnumerable<TeamDto>>> GetByPortfolioId(Guid portfolioId)
        {
            var teams = await _teamService.GetByPortfolioIdAsync(portfolioId);
            return Ok(teams);
        }

        [HttpPost]
        public async Task<ActionResult<TeamDto>> Create([FromBody] CreateTeamDto createDto)
        {
            if (createDto == null || string.IsNullOrWhiteSpace(createDto.Name))
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                var createdTeam = await _teamService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = createdTeam.Id }, createdTeam);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamDto updateDto)
        {
            if (updateDto == null || id != updateDto.Id)
            {
                return BadRequest("ID mismatch or invalid payload.");
            }

            var existingTeam = await _teamService.GetByIdAsync(id);
            if (existingTeam == null)
            {
                return NotFound();
            }

            try
            {
                await _teamService.UpdateAsync(updateDto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingTeam = await _teamService.GetByIdAsync(id);
            if (existingTeam == null)
            {
                return NotFound();
            }

            await _teamService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id:guid}/members")]
        public async Task<ActionResult<TeamDto>> AddMember(Guid id, [FromBody] AddTeamMemberDto addMemberDto)
        {
            if (addMemberDto == null)
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                var team = await _teamService.AddMemberAsync(id, addMemberDto);
                if (team == null)
                {
                    return NotFound();
                }

                return Ok(team);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{id:guid}/members/{userId:guid}")]
        public async Task<ActionResult<TeamDto>> RemoveMember(Guid id, Guid userId)
        {
            try
            {
                var team = await _teamService.RemoveMemberAsync(id, userId);
                if (team == null)
                {
                    return NotFound();
                }

                return Ok(team);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Self-service join: adds the calling (JWT) user to the team identified by the given
        /// invite code, without requiring the team owner/manager to explicitly add them.
        /// </summary>
        [HttpPost("join")]
        public async Task<ActionResult<TeamDto>> Join([FromBody] JoinTeamDto joinDto)
        {
            if (joinDto == null || string.IsNullOrWhiteSpace(joinDto.Code))
            {
                return BadRequest("Code is required.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return Unauthorized("User id claim is missing or invalid.");
            }

            try
            {
                var team = await _teamService.JoinAsync(userIdGuid, joinDto.Code);
                return Ok(team);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        /// <summary>
        /// Returns the team's invite code so its owner can share it with people who should join.
        /// </summary>
        [HttpGet("{id:guid}/code")]
        public async Task<ActionResult<TeamInviteCodeDto>> GetInviteCode(Guid id)
        {
            var code = await _teamService.GetInviteCodeAsync(id);
            if (code == null)
            {
                return NotFound();
            }

            return Ok(new TeamInviteCodeDto { Code = code });
        }
    }
}
