namespace Prumo.Application.Interfaces
{
    public interface IPrioritizationService
    {
        /// <summary>
        /// F3 (RF21): recalcula F1 e F2 para os projetos com status de avaliação Priorizado, Aprovado
        /// ou Reavaliado que estejam com todas as notas completas. Chamado depois de gravar notas,
        /// alterar peso/tipo de critério e mudar o status de um projeto.
        /// </summary>
        Task RecalculateIfNeededAsync(Guid portfolioId);
    }
}
