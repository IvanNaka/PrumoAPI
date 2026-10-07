using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Auth;
using Prumo.Application.DTOs.Onboarding;
using Prumo.Application.DTOs.Team;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    public class OnboardingService : IOnboardingService
    {
        // Capacidade padrão do membro criado no onboarding; o TechLead/Administrador ajusta depois.
        private const int DefaultMonthlyCapacityHours = 160;

        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly ITeamService _teamService;
        private readonly IAuthService _authService;

        public OnboardingService(IAppDbContext db, ICurrentUserService currentUser, ITeamService teamService, IAuthService authService)
        {
            _db = db;
            _currentUser = currentUser;
            _teamService = teamService;
            _authService = authService;
        }

        public async Task<LoginResponseDto> CreateTeamAsync(CriarEquipeOnboardingDto dto)
        {
            var user = await RequirePendingUserAsync();
            var team = await _teamService.CreateAsync(new SalvarEquipeDto { Nome = dto.Nome });

            AddMembership(team.Id, user);
            user.Roles.Add(new UserRole { UserId = user.Id, Role = RoleName.TechLead });
            await _db.SaveChangesAsync();

            return await _authService.RefreshSessionAsync(user.Id);
        }

        public async Task<LoginResponseDto> JoinTeamAsync(EntrarEquipeDto dto)
        {
            // Quem já participa do Prumo também pode entrar em outras equipes pelo código;
            // nesse caso mantém os perfis que já tem.
            var user = await RequireActiveUserAsync();
            var codigo = dto.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;
            if (codigo.Length == 0)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios,
                    new Dictionary<string, string[]> { ["codigo"] = new[] { "O código de convite é obrigatório." } });
            }

            var team = await _db.Teams.SingleOrDefaultAsync(t => t.InviteCode == codigo)
                ?? throw Messages.NotFound(Messages.CodigoConviteInvalido);

            var existing = await _db.TeamUsers.SingleOrDefaultAsync(m => m.TeamId == team.Id && m.Email == user.Email);
            if (existing is null)
            {
                AddMembership(team.Id, user);
            }
            else if (existing.UserId == user.Id)
            {
                throw new BusinessRuleException(409, Messages.JaMembroEquipe);
            }
            else
            {
                existing.UserId = user.Id;
            }

            if (user.Roles.Count == 0)
            {
                user.Roles.Add(new UserRole { UserId = user.Id, Role = RoleName.Desenvolvedor });
            }
            await _db.SaveChangesAsync();

            return await _authService.RefreshSessionAsync(user.Id);
        }

        /// <summary>Somente quem ainda não tem perfil passa pelo onboarding.</summary>
        private async Task<User> RequirePendingUserAsync()
        {
            var user = await RequireActiveUserAsync();
            if (user.Roles.Count > 0)
            {
                throw new BusinessRuleException(409, Messages.JaParticipa);
            }

            return user;
        }

        private async Task<User> RequireActiveUserAsync()
        {
            var userId = _currentUser.RequireUserId();
            var user = await _db.Users.Include(u => u.Roles).SingleOrDefaultAsync(u => u.Id == userId);
            if (user is null || !user.IsActive)
            {
                throw new BusinessRuleException(403, Messages.RN03_SemPermissao);
            }

            return user;
        }

        private void AddMembership(Guid teamId, User user)
        {
            _db.TeamUsers.Add(new TeamUser
            {
                TeamId = teamId,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                HourlyCost = 0,
                MonthlyCapacityHours = DefaultMonthlyCapacityHours,
            });
        }
    }
}
