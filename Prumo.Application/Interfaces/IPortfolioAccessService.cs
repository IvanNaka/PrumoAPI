namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Controle de acesso ao portfólio (Seção 3.6 / D11). Chamado no início de todo método de
    /// serviço que recebe um portfolioId ou um projetoId.
    /// </summary>
    public interface IPortfolioAccessService
    {
        /// <summary>Administrador e Diretoria veem todos os portfólios.</summary>
        bool CanSeeAll { get; }

        /// <summary>GarantirAcesso: exige ser membro ou responsável (exceto Administrador/Diretoria); senão RN06.</summary>
        Task EnsureAccessAsync(Guid portfolioId);

        /// <summary>GarantirNaoEncerrado: portfólio Encerrado -> RN23.</summary>
        Task EnsureNotClosedAsync(Guid portfolioId);

        /// <summary>Acesso de escrita: <see cref="EnsureAccessAsync"/> + <see cref="EnsureNotClosedAsync"/>.</summary>
        Task EnsureWriteAccessAsync(Guid portfolioId);

        /// <summary>Busca o portfólio do projeto, garante o acesso e devolve o portfolioId (404 se o projeto não existir).</summary>
        Task<Guid> EnsureProjectAccessAsync(Guid projectId, bool write = false);

        /// <summary>Somente o responsável do portfólio ou o Administrador (membros do portfólio).</summary>
        Task EnsureOwnerOrAdminAsync(Guid portfolioId);
    }
}
