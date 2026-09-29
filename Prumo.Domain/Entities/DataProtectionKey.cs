namespace Prumo.Domain.Entities
{
    /// <summary>
    /// Chaves do ASP.NET Data Protection guardadas no banco, para que o token do Jira continue
    /// legível depois de reinícios e entre instâncias da API.
    /// </summary>
    public class DataProtectionKey
    {
        public int Id { get; set; }
        public string? FriendlyName { get; set; }
        public string Xml { get; set; } = string.Empty;
    }
}
