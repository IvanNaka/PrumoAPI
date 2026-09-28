using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Roadmap;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoadmapsController : ControllerBase
    {
        private readonly IRoadmapService _roadmapService;
        private readonly IPortfolioService _portfolioService;

        public RoadmapsController(IRoadmapService roadmapService, IPortfolioService portfolioService)
        {
            _roadmapService = roadmapService;
            _portfolioService = portfolioService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RoadmapItemDto>> GetById(Guid id)
        {
            var item = await _roadmapService.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpGet("portfolio/{portfolioId:guid}")]
        public async Task<ActionResult<IEnumerable<RoadmapItemDto>>> GetByPortfolioId(Guid portfolioId)
        {
            var portfolio = await _portfolioService.GetByIdAsync(portfolioId);
            if (portfolio == null)
            {
                return NotFound();
            }

            var items = await _roadmapService.GetByPortfolioIdAsync(portfolioId);
            return Ok(items);
        }

        [HttpPost]
        public async Task<ActionResult<RoadmapItemDto>> Create([FromBody] CreateRoadmapItemDto createDto)
        {
            if (createDto == null)
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                var created = await _roadmapService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoadmapItemDto updateDto)
        {
            if (updateDto == null || id != updateDto.Id)
            {
                return BadRequest("ID mismatch or invalid payload.");
            }

            try
            {
                await _roadmapService.UpdateAsync(updateDto);
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
            var existing = await _roadmapService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            await _roadmapService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut("portfolio/{portfolioId:guid}/reorder")]
        public async Task<IActionResult> Reorder(Guid portfolioId, [FromBody] ReorderRoadmapDto reorderDto)
        {
            if (reorderDto == null)
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                await _roadmapService.ReorderAsync(portfolioId, reorderDto);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
