using Prumo.Application.DTOs.Auth;
using Prumo.Application.DTOs.Onboarding;

namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Primeiro acesso: o usuário sem perfil entra em uma equipe (código de convite) ou cria uma.
    /// Devolve um novo JWT já com o perfil concedido.
    /// </summary>
    public interface IOnboardingService
    {
        /// <summary>Cria a equipe e torna o usuário TechLead e membro dela.</summary>
        Task<LoginResponseDto> CreateTeamAsync(CriarEquipeOnboardingDto dto);

        /// <summary>Entra na equipe do código de convite como Desenvolvedor.</summary>
        Task<LoginResponseDto> JoinTeamAsync(EntrarEquipeDto dto);
    }
}
