namespace Prumo.Application.DTOs.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Perfis { get; set; } = new();
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
