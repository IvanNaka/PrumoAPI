using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Prioritization;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Avaliação, score e ranking (RF18–RF21, UC10, Figura 28).
    [ApiController]
    [Authorize]
    [Route("api")]
    public class PrioritizationController : ControllerBase
    {
        private readonly IPrioritizationService _service;

        public PrioritizationController(IPrioritizationService service)
        {
            _service = service;
        }

        /// <summary>Grava ou atualiza as notas do projeto: [{ criterioId, nota }].</summary>
        [HttpPut("projetos/{id:guid}/avaliacoes")]
        [Authorize(Policy = Policies.Priorizar)]
        public async Task<ActionResult<AvaliacaoProjetoDto>> SaveEvaluations(Guid id, [FromBody] List<NotaInputDto> notas)
        {
            return Ok(await _service.SaveEvaluationsAsync(id, notas ?? new List<NotaInputDto>()));
        }

        /// <summary>Calcula F1 e F2 e devolve { ranking, naoAvaliados }.</summary>
        [HttpPost("portfolios/{portfolioId:guid}/priorizacao")]
        [Authorize(Policy = Policies.Priorizar)]
        public async Task<ActionResult<PriorizacaoResultadoDto>> Prioritize(Guid portfolioId)
        {
            return Ok(await _service.PrioritizeAsync(portfolioId));
        }

        [HttpGet("portfolios/{portfolioId:guid}/ranking")]
        public async Task<ActionResult<PriorizacaoResultadoDto>> Ranking(Guid portfolioId)
        {
            return Ok(await _service.GetRankingAsync(portfolioId));
        }

        /// <summary>Matriz projetos x critérios com as notas (aba "Avaliar").</summary>
        [HttpGet("portfolios/{portfolioId:guid}/avaliacoes")]
        public async Task<ActionResult<MatrizAvaliacaoDto>> Matrix(Guid portfolioId)
        {
            return Ok(await _service.GetMatrixAsync(portfolioId));
        }

        [HttpPost("projetos/{id:guid}/avaliacao/{acao}")]
        [Authorize(Policy = Policies.Priorizar)]
        public async Task<ActionResult<AvaliacaoProjetoDto>> Decide(Guid id, string acao)
        {
            return Ok(await _service.DecideAsync(id, acao));
        }
    }
}
