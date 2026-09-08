using Prumo.Application.Interfaces;
using Prumo.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Prumo.Application.Services
{
    public class IntegrationProviderFactory : IIntegrationProviderFactory
    {
        private readonly Dictionary<IntegrationType, IIntegrationProvider> _providers;

        public IntegrationProviderFactory(IEnumerable<IIntegrationProvider> providers)
        {
            _providers = providers.ToDictionary(p => p.Type);
        }

        public IIntegrationProvider GetProvider(IntegrationType type)
        {
            return _providers.TryGetValue(type, out var provider) ? provider : null;
        }
    }
}
