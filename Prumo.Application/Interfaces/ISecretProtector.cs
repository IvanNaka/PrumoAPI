namespace Prumo.Application.Interfaces
{
    /// <summary>Criptografia do token do Jira (ASP.NET Data Protection, propósito "Prumo.Jira").</summary>
    public interface ISecretProtector
    {
        string Protect(string plainText);
        string Unprotect(string protectedText);
    }
}
