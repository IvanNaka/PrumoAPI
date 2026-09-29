using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Plantonize.Plantao.Infrastructure;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;

namespace Prumo.API.Security
{
    /// <summary>Criptografa o token do Jira com o propósito "Prumo.Jira".</summary>
    public class DataProtectionSecretProtector : ISecretProtector
    {
        private readonly IDataProtector _protector;

        public DataProtectionSecretProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Prumo.Jira");
        }

        public string Protect(string plainText) => _protector.Protect(plainText);

        public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
    }

    /// <summary>
    /// Guarda as chaves do Data Protection na tabela DataProtectionKeys, para que o token continue
    /// legível depois de reinícios do container.
    /// </summary>
    public class DbXmlRepository : IXmlRepository
    {
        private readonly IServiceScopeFactory _scopes;

        public DbXmlRepository(IServiceScopeFactory scopes)
        {
            _scopes = scopes;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PrumoDbContext>();
            return db.DataProtectionKeys.AsEnumerable().Select(k => XElement.Parse(k.Xml)).ToList();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PrumoDbContext>();
            db.DataProtectionKeys.Add(new DataProtectionKey { FriendlyName = friendlyName, Xml = element.ToString(SaveOptions.DisableFormatting) });
            db.SaveChanges();
        }
    }
}
