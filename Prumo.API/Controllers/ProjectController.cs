using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Project;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProjectDto>> GetById(Guid id)
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return Ok(project);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return Unauthorized("User id claim is missing or invalid.");
            }
            var projects = await _projectService.GetByOwnerIdAsync(userIdGuid);
            return Ok(projects);
        }

        [HttpGet("portfolio/{portfolioId:guid}")]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetByPortfolioId(Guid portfolioId)
        {
            var projects = await _projectService.GetByPortfolioIdAsync(portfolioId);
            return Ok(projects);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDTO project)
        {
            if (project == null)
            {
                return BadRequest();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return Unauthorized("User id claim is missing or invalid.");
            }

            project.OwnerId = userIdGuid;

            var createdProject = await _projectService.AddAsync(project);

            return CreatedAtAction(nameof(GetById), new { id = createdProject.Id }, createdProject);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Project project)
        {
            if (project == null || project.Id != id)
            {
                return BadRequest();
            }

            await _projectService.UpdateAsync(project);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingProject = await _projectService.GetByIdAsync(id);
            if (existingProject == null)
            {
                return NotFound();
            }

            await _projectService.DeleteAsync(id);
            return NoContent();
        }
    }
}