using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Report;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Relatórios (RF43, RF44): PDF e Excel, com histórico.
    [ApiController]
    [Authorize(Policy = Policies.Relatorios)]
    [Route("api/portfolios/{id:guid}/relatorios")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportsController(IReportService service)
        {
            _service = service;
        }

        /// <summary>tipo = portfolio | executivo; ?formato=pdf|excel.</summary>
        [HttpGet("{tipo:regex(^(portfolio|executivo)$)}")]
        public async Task<IActionResult> Generate(Guid id, string tipo, [FromQuery] string? formato)
        {
            var file = await _service.GenerateAsync(id, tipo, formato);
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet("historico")]
        public async Task<ActionResult<IEnumerable<RelatorioDto>>> History(Guid id)
        {
            return Ok(await _service.HistoryAsync(id));
        }
    }
}
