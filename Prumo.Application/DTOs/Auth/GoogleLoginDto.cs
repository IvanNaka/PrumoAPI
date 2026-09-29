namespace Prumo.Application.DTOs.Auth
{
    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public AuthUserDto Usuario { get; set; } = new();
    }

    public class AuthUserDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Perfis { get; set; } = new();
    }
}
