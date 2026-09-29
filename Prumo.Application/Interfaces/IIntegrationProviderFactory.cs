using Prumo.Domain.Enums;

namespace Prumo.Application.Interfaces
{
    /// <summary>Resolve o <see cref="IIntegrationProvider"/> de cada ferramenta.</summary>
    public interface IIntegrationProviderFactory
    {
        /// <summary>Devolve o provider da ferramenta, ou null se não houver.</summary>
        IIntegrationProvider? GetProvider(IntegrationType type);
    }
}
