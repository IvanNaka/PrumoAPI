using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.PriorityCriteria;
using Prumo.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PriorityCriteriaController : ControllerBase
    {
        private readonly IPriorityCriteriaService _priorityCriteriaService;

        public PriorityCriteriaController(IPriorityCriteriaService priorityCriteriaService)
        {
            _priorityCriteriaService = priorityCriteriaService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PriorityCriteriaDto>> GetById(Guid id)
        {
            var criteria = await _priorityCriteriaService.GetByIdAsync(id);
            if (criteria == null)
            {
                return NotFound();
            }

            return Ok(criteria);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PriorityCriteriaDto>>> GetAll()
        {
            var criteriaList = await _priorityCriteriaService.GetAllAsync();
            return Ok(criteriaList);
        }

        [HttpGet("portfolio/{portfolioId:guid}")]
        public async Task<ActionResult<IEnumerable<PriorityCriteriaDto>>> GetByPortfolioId(Guid portfolioId)
        {
            var criteriaList = await _priorityCriteriaService.GetByPortfolioIdAsync(portfolioId);
            return Ok(criteriaList);
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult<IEnumerable<PriorityCriteriaDto>>> GetByUserId(Guid userId)
        {
            var criteriaList = await _priorityCriteriaService.GetByUserIdAsync(userId);
            return Ok(criteriaList);
        }

        [HttpPost]
        public async Task<ActionResult<PriorityCriteriaDto>> Create([FromBody] CreatePriorityCriteriaDto createDto)
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

            createDto.UserId = userIdGuid;
            var createdCriteria = await _priorityCriteriaService.CreateAsync(createDto);

            return CreatedAtAction(nameof(GetById), new { id = createdCriteria.Id }, createdCriteria);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePriorityCriteriaDto updateDto)
        {
            if (updateDto == null || id != updateDto.Id)
            {
                return BadRequest("ID mismatch or invalid payload.");
            }

            var existingCriteria = await _priorityCriteriaService.GetByIdAsync(id);
            if (existingCriteria == null)
            {
                return NotFound();
            }

            await _priorityCriteriaService.UpdateAsync(updateDto);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingCriteria = await _priorityCriteriaService.GetByIdAsync(id);
            if (existingCriteria == null)
            {
                return NotFound();
            }

            await _priorityCriteriaService.DeleteAsync(id);

            return NoContent();
        }
    }
}