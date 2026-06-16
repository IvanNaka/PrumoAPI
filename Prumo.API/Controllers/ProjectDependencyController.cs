using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Prumo.Application.DTOs.ProjectDependency;
using Prumo.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectDependencyController : ControllerBase
    {
        private readonly IProjectDependencyService _projectDependencyService;

        public ProjectDependencyController(IProjectDependencyService projectDependencyService)
        {
            _projectDependencyService = projectDependencyService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProjectDependencyDto>> GetById(Guid id)
        {
            var projectDependency = await _projectDependencyService.GetByIdAsync(id);
            if (projectDependency == null)
            {
                return NotFound();
            }

            return Ok(projectDependency);
        }

        [HttpGet("portfolio/{portfolioId:guid}")]
        public async Task<ActionResult<IEnumerable<ProjectDependencyDto>>> GetAllByPortfolioId(Guid portfolioId)
        {
            var projectDependencies = await _projectDependencyService.GetAllByPortfolioAsync(portfolioId);
            return Ok(projectDependencies);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectDependencyDto>> Create([FromBody] CreateProjectDependencyDto createDto)
        {
            if (createDto == null)
            {
                return BadRequest("Invalid payload.");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return Unauthorized("User id claim is missing or invalid.");
            }
            var createdProjectDependency = await _projectDependencyService.CreateAsync(createDto);

            return CreatedAtAction(nameof(GetById), new { id = createdProjectDependency.Id }, createdProjectDependency);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingProjectDependency = await _projectDependencyService.GetByIdAsync(id);
            if (existingProjectDependency == null)
            {
                return NotFound();
            }

            await _projectDependencyService.DeleteAsync(id);

            return NoContent();
        }
    }
}