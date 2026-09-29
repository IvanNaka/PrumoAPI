using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Integration;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Integração Jira (RF47, RF51, UC15, UC16).
    [ApiController]
    [Authorize(Policy = Policies.Integracoes)]
    [Route("api/integracoes/jira")]
    public class IntegrationsController : ControllerBase
    {
        private readonly IIntegrationService _service;
        private readonly IIntegrationSyncService _sync;

        public IntegrationsController(IIntegrationService service, IIntegrationSyncService sync)
        {
            _service = service;
            _sync = sync;
        }

        /// <summary>Configuração atual — nunca devolve o token.</summary>
        [HttpGet]
        public async Task<ActionResult<IntegracaoJiraDto>> Get()
        {
            return Ok(await _service.GetJiraAsync());
        }

        /// <summary>Salva a configuração e testa a conexão automaticamente.</summary>
        [HttpPut]
        public async Task<ActionResult<IntegracaoJiraDto>> Save([FromBody] SalvarIntegracaoJiraDto dto)
        {
            return Ok(await _service.SaveJiraAsync(dto));
        }

        [HttpPost("testar")]
        public async Task<ActionResult<IntegracaoJiraDto>> Test()
        {
            return Ok(await _service.TestJiraAsync());
        }

        /// <summary>UC16: dispara a sincronização em segundo plano (202).</summary>
        [HttpPost("sincronizar")]
        public async Task<ActionResult<IntegracaoJiraDto>> Sync()
        {
            return Accepted(await _sync.RequestJiraSyncAsync());
        }

        [HttpGet("logs")]
        public async Task<ActionResult<IEnumerable<SincronizacaoLogDto>>> Logs()
        {
            return Ok(await _service.GetJiraLogsAsync());
        }
    }
}
