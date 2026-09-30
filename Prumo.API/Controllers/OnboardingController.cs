using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.Application.DTOs.Auth;
using Prumo.Application.DTOs.Onboarding;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Primeiro acesso: liberado para usuários logados ainda sem perfil.
    [ApiController]
    [Authorize(Policy = Policies.Autenticado)]
    [Route("api/onboarding")]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        /// <summary>Cria uma equipe; o usuário vira TechLead. Devolve um novo JWT.</summary>
        [HttpPost("equipes")]
        public async Task<ActionResult<LoginResponseDto>> CreateTeam([FromBody] CriarEquipeOnboardingDto dto)
        {
            return Ok(await _onboardingService.CreateTeamAsync(dto));
        }

        /// <summary>Entra em uma equipe pelo código de convite; o usuário vira Desenvolvedor. Devolve um novo JWT.</summary>
        [HttpPost("entrar")]
        public async Task<ActionResult<LoginResponseDto>> JoinTeam([FromBody] EntrarEquipeDto dto)
        {
            return Ok(await _onboardingService.JoinTeamAsync(dto));
        }
    }
}
