namespace Prumo.Application.DTOs.Onboarding
{
    // POST /onboarding/equipes — { nome }
    public class CriarEquipeOnboardingDto
    {
        public string? Nome { get; set; }
    }

    // POST /onboarding/entrar — { codigo }
    public class EntrarEquipeDto
    {
        public string? Codigo { get; set; }
    }
}
