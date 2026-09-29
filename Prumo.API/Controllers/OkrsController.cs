using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Okr;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // OKRs e Key Results (RF14, RF15, RF17, UC8).
    [ApiController]
    [Authorize]
    [Route("api")]
    public class OkrsController : ControllerBase
    {
        private readonly IOkrService _service;

        public OkrsController(IOkrService service)
        {
            _service = service;
        }

        /// <summary>Lista de OKRs com o progresso (F4) de cada OKR e KR.</summary>
        [HttpGet("okrs")]
        public async Task<ActionResult<IEnumerable<OkrDto>>> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("okrs/{id:guid}")]
        public async Task<ActionResult<OkrDto>> Get(Guid id)
        {
            return Ok(await _service.GetAsync(id));
        }

        [HttpPost("okrs")]
        [Authorize(Policy = Policies.EditarOkrs)]
        public async Task<ActionResult<OkrDto>> Create([FromBody] SalvarOkrDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("okrs/{id:guid}")]
        [Authorize(Policy = Policies.EditarOkrs)]
        public async Task<ActionResult<OkrDto>> Update(Guid id, [FromBody] SalvarOkrDto dto)
        {
            return Ok(await _service.UpdateAsync(id, dto));
        }

        /// <summary>Atualiza o valor atual do Key Result: { valorAtual }.</summary>
        [HttpPut("key-results/{id:guid}")]
        [Authorize(Policy = Policies.EditarOkrs)]
        public async Task<ActionResult<KeyResultDto>> UpdateKeyResult(Guid id, [FromBody] AtualizarValorKrDto dto)
        {
            return Ok(await _service.UpdateKeyResultValueAsync(id, dto?.ValorAtual));
        }
    }
}
