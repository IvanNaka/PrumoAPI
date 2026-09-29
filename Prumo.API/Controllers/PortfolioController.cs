using Microsoft.AspNetCore.Authorization;
using Prumo.API.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Prumo.Application.DTOs.Portfolio;
using Prumo.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PortfoliosController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IPortfolioAccessService _access;

        public PortfoliosController(IPortfolioService portfolioService, IPortfolioAccessService access)
        {
            _portfolioService = portfolioService;
            _access = access;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PortfolioDto>> GetById(Guid id)
        {
            await _access.EnsureAccessAsync(id);
            var portfolio = await _portfolioService.GetByIdAsync(id);
            if (portfolio == null)
            {
                return NotFound();
            }

            return Ok(portfolio);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PortfolioDto>>> GetAllByOwnerId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return Unauthorized("User id claim is missing or invalid.");
            }
            var portfolios = await _portfolioService.GetByOwnerIdAsync(Guid.Parse(userId));
            return Ok(portfolios);
        }

        [Authorize(Policy = Policies.EditarPortfolio)]
        [HttpPost]
        public async Task<ActionResult<PortfolioDto>> Create([FromBody] CreatePortfolioDto createDto)
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
            createDto.OwnerId = userIdGuid;
            var createdPortfolio = await _portfolioService.CreateAsync(createDto);

            return CreatedAtAction(nameof(GetById), new { id = createdPortfolio.Id }, createdPortfolio);
        }

        [Authorize(Policy = Policies.EditarPortfolio)]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePortfolioDto updateDto)
        {
            if (updateDto == null || id != updateDto.Id)
            {
                return BadRequest("ID mismatch or invalid payload.");
            }

            var existingPortfolio = await _portfolioService.GetByIdAsync(id);
            if (existingPortfolio == null)
            {
                return NotFound();
            }
            await _portfolioService.UpdateAsync(updateDto);

            return NoContent();
        }

        [Authorize(Policy = Policies.EditarPortfolio)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingPortfolio = await _portfolioService.GetByIdAsync(id);
            if (existingPortfolio == null)
            {
                return NotFound();
            }

            await _portfolioService.DeleteAsync(id);

            return NoContent();
        }
    }
}