using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Portfolio;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Dashboard do portfólio (RF35–RF42, UC11): todos os perfis, desde que tenham acesso ao portfólio (RN06).
    [ApiController]
    [Authorize]
    [Route("api/portfolios/{id:guid}")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardDto>> Get(Guid id, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
        {
            return Ok(await _service.GetAsync(id, de, ate));
        }

        [HttpGet("indicadores/{nome}")]
        public async Task<ActionResult<object>> Indicator(Guid id, string nome, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate)
        {
            // object: serializa o tipo concreto do indicador.
            return Ok((object)await _service.GetIndicatorAsync(id, nome, de, ate));
        }
    }
}
