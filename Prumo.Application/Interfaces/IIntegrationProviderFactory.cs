using Prumo.Domain.Enums;

namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Resolves the concrete <see cref="IIntegrationProvider"/> implementation for a given
    /// <see cref="IntegrationType"/>, so the rest of the application stays generic/tool-agnostic.
    /// </summary>
    public interface IIntegrationProviderFactory
    {
        /// <summary>Returns the provider for the given tool, or null if none is registered.</summary>
        IIntegrationProvider GetProvider(IntegrationType type);
    }
}
