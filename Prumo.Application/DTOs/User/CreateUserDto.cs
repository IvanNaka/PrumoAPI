namespace Prumo.Application.DTOs.User
{
    // POST /usuarios — { nome, email, perfis[] }
    public class CreateUserDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Perfis { get; set; } = new();
    }

    // PUT /usuarios/{id} — { nome, perfis[] }
    public class UpdateUserDto
    {
        public string Nome { get; set; } = string.Empty;
        public List<string> Perfis { get; set; } = new();
    }

    // PATCH /usuarios/{id}/ativo — { ativo }
    public class SetUserActiveDto
    {
        public bool Ativo { get; set; }
    }
}
