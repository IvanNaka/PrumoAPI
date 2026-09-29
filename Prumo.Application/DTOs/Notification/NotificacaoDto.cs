namespace Prumo.Application.DTOs.Notification
{
    public class NotificacaoDto
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string? EntidadeTipo { get; set; }
        public Guid? EntidadeId { get; set; }

        /// <summary>Projeto relacionado (para "Dependencia": o projeto de origem), usado no link da central.</summary>
        public Guid? ProjetoId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataEnvio { get; set; }
        public DateTime? DataLeitura { get; set; }
        public DateTime? DataArquivamento { get; set; }
    }

    public class ContagemDto
    {
        public int Quantidade { get; set; }
    }
}
