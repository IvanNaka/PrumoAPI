using Microsoft.AspNetCore.Authorization;
using Prumo.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Integration;
using Prumo.Application.Exceptions;
using Prumo.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.API.Controllers
{
    /// <summary>
    /// UC15 "Configurar Integração" / UC16 "Sincronizar Dados": endpoints to configure and sync
    /// external tool integrations (Jira, Azure DevOps, GitHub, Trello - RF47/RF48/RF49/RF50).
    /// </summary>
    [ApiController]
    [Authorize(Policy = Policies.Integracoes)]
    [Route("api/[controller]")]
    public class IntegrationsController : ControllerBase
    {
        private readonly IIntegrationService _integrationService;

        public IntegrationsController(IIntegrationService integrationService)
        {
            _integrationService = integrationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IntegrationDto>>> GetAll()
        {
            var integrations = await _integrationService.GetAllAsync();
            return Ok(integrations);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<IntegrationDto>> GetById(Guid id)
        {
            var integration = await _integrationService.GetByIdAsync(id);
            if (integration == null)
            {
                return NotFound();
            }

            return Ok(integration);
        }

        [HttpPost]
        public async Task<ActionResult<IntegrationDto>> Configure([FromBody] ConfigureIntegrationDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ApiUrl) || string.IsNullOrWhiteSpace(dto.Token))
            {
                return BadRequest("Invalid payload.");
            }

            try
            {
                var integration = await _integrationService.ConfigureAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = integration.Id }, integration);
            }
            catch (IntegrationAuthenticationException ex)
            {
                // 422, not 401: these are the *external* integration's credentials, not the
                // caller's own session/JWT. Returning 401 here would collide with the front-end's
                // auth interceptor and incorrectly log the user out of the application.
                return UnprocessableEntity(ex.Message);
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<IntegrationDto>> Update(Guid id, [FromBody] UpdateIntegrationDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Invalid payload.");
            }

            var updated = await _integrationService.UpdateAsync(id, dto);
            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _integrationService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            await _integrationService.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>
        /// UC16 "Sincronizar Dados": manually triggers a sync for the given integration. The same
        /// operation also runs automatically per <see cref="IntegrationDto.SyncIntervalMinutes"/> (RF51).
        /// </summary>
        [HttpPost("{id:guid}/sync")]
        public async Task<ActionResult<IntegrationSyncResultDto>> Sync(Guid id)
        {
            try
            {
                var result = await _integrationService.SyncAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (IntegrationAuthenticationException ex)
            {
                // 422, not 401: these are the *external* integration's credentials, not the
                // caller's own session/JWT. Returning 401 here would collide with the front-end's
                // auth interceptor and incorrectly log the user out of the application.
                return UnprocessableEntity(ex.Message);
            }
            catch (IntegrationUnavailableException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
