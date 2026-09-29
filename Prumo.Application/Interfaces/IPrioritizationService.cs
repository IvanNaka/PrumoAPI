using Prumo.Application.DTOs.Prioritization;

namespace Prumo.Application.Interfaces
{
    // PriorizacaoService — UC10, RF18–RF21, Figura 28, F1–F3.
    public interface IPrioritizationService
    {
        /// <summary>
        /// F3 (RF21): recalcula F1 e F2 para os projetos com status de avaliação Priorizado, Aprovado
        /// ou Reavaliado que estejam com todas as notas completas. Chamado depois de gravar notas,
        /// alterar peso/tipo de critério e mudar o status de um projeto.
        /// </summary>
        Task RecalculateIfNeededAsync(Guid portfolioId);

        /// <summary>PUT /projetos/{id}/avaliacoes — grava ou atualiza as notas (RN17).</summary>
        Task<AvaliacaoProjetoDto> SaveEvaluationsAsync(Guid projectId, IReadOnlyCollection<NotaInputDto> notas);

        /// <summary>POST /portfolios/{id}/priorizacao — F1 + F2 (RN15, RN16).</summary>
        Task<PriorizacaoResultadoDto> PrioritizeAsync(Guid portfolioId);

        /// <summary>GET /portfolios/{id}/ranking.</summary>
        Task<PriorizacaoResultadoDto> GetRankingAsync(Guid portfolioId);

        /// <summary>Matriz de notas (aba "Avaliar").</summary>
        Task<MatrizAvaliacaoDto> GetMatrixAsync(Guid portfolioId);

        /// <summary>POST /projetos/{id}/avaliacao/{aprovar|rejeitar} (RN29).</summary>
        Task<AvaliacaoProjetoDto> DecideAsync(Guid projectId, string acao);
    }
}
