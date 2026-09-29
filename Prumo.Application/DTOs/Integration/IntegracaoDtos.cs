namespace Prumo.Application.DTOs.Integration
{
    /// <summary>GET /integracoes/jira — nunca devolve o token.</summary>
    public class IntegracaoJiraDto
    {
        public bool Configurada { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int IntervaloSincronizacaoMinutos { get; set; } = 60;
        public bool Ativo { get; set; }
        public string Status { get; set; } = "NaoConfigurada";
        public DateTime? UltimaSincronizacao { get; set; }
        public int TentativasFalhas { get; set; }
        public DateTime? ProximaTentativa { get; set; }

        /// <summary>Indica se há token salvo (o token em si nunca sai da API).</summary>
        public bool TokenConfigurado { get; set; }
    }

    // PUT /integracoes/jira — { url, email, apiToken, intervaloSincronizacaoMinutos, ativo }
    public class SalvarIntegracaoJiraDto
    {
        public string? Url { get; set; }
        public string? Email { get; set; }

        /// <summary>Em edição, vazio = manter o token atual.</summary>
        public string? ApiToken { get; set; }
        public int? IntervaloSincronizacaoMinutos { get; set; }
        public bool? Ativo { get; set; }
    }

    public class SincronizacaoLogDto
    {
        public Guid Id { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime? Fim { get; set; }
        public bool Sucesso { get; set; }
        public int IssuesProcessadas { get; set; }
        public int WorklogsProcessados { get; set; }
        public string? MensagemErro { get; set; }
    }

    /// <summary>Credenciais já descriptografadas, usadas só dentro da API.</summary>
    public record IntegrationCredentials(string Url, string Email, string ApiToken);
}
