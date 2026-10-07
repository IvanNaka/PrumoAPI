using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Team;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // Equipes e membros (RF29–RF31) e capacidade (RF32, RF39, UC13, F9).
    public class TeamService : ITeamService
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IndicatorDataLoader _loader;

        public TeamService(IAppDbContext db, ICurrentUserService currentUser, IndicatorDataLoader loader)
        {
            _db = db;
            _currentUser = currentUser;
            _loader = loader;
        }

        public async Task<IEnumerable<EquipeDto>> GetAllAsync()
        {
            var query = _db.Teams.AsNoTracking()
                .Include(t => t.Members)
                .Include(t => t.Portfolio)
                .AsQueryable();

            // Administrador vê todas; os demais veem só as equipes de que são membros (pelo usuário ou
            // pelo e-mail) ou que criaram.
            if (!_currentUser.IsInRole(RoleName.Administrador))
            {
                var userId = _currentUser.RequireUserId();
                var email = await _db.Users.AsNoTracking().Where(u => u.Id == userId)
                    .Select(u => u.Email.ToLower()).SingleOrDefaultAsync() ?? string.Empty;
                query = query.Where(t => t.OwnerUserId == userId
                    || t.Members.Any(m => m.UserId == userId || (email != string.Empty && m.Email == email)));
            }

            var teams = await query.OrderBy(t => t.Name).ToListAsync();
            return teams.Select(t => Map(t, CanSeeInviteCode()));
        }

        public async Task<EquipeDto> GetAsync(Guid id)
        {
            var team = await _db.Teams.AsNoTracking()
                .Include(t => t.Members)
                .Include(t => t.Portfolio)
                .SingleOrDefaultAsync(t => t.Id == id)
                ?? throw Messages.NotFound("Equipe não encontrada.");
            return Map(team, CanSeeInviteCode());
        }

        public async Task<EquipeDto> CreateAsync(SalvarEquipeDto dto)
        {
            var nome = await ValidateTeamAsync(dto, null);
            var team = new Team { Name = nome, PortfolioId = dto.PortfolioId, OwnerUserId = _currentUser.UserId };
            _db.Teams.Add(team);
            await _db.SaveChangesAsync();
            return await GetAsync(team.Id);
        }

        public async Task<EquipeDto> UpdateAsync(Guid id, SalvarEquipeDto dto)
        {
            var team = await _db.Teams.SingleOrDefaultAsync(t => t.Id == id) ?? throw Messages.NotFound("Equipe não encontrada.");
            team.Name = await ValidateTeamAsync(dto, id);
            team.PortfolioId = dto.PortfolioId;
            team.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await GetAsync(id);
        }

        public async Task DeleteAsync(Guid id)
        {
            var team = await _db.Teams.SingleOrDefaultAsync(t => t.Id == id) ?? throw Messages.NotFound("Equipe não encontrada.");
            _db.Teams.Remove(team);
            await _db.SaveChangesAsync();
        }

        public async Task<EquipeDto> RegenerateInviteCodeAsync(Guid id)
        {
            var team = await _db.Teams.SingleOrDefaultAsync(t => t.Id == id) ?? throw Messages.NotFound("Equipe não encontrada.");
            team.InviteCode = Team.NewInviteCode();
            team.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await GetAsync(id);
        }

        public async Task<IEnumerable<MembroEquipeDto>> GetMembersAsync(Guid teamId)
        {
            await EnsureTeamAsync(teamId);
            var members = await _db.TeamUsers.AsNoTracking().Where(m => m.TeamId == teamId).OrderBy(m => m.Name).ToListAsync();
            return members.Select(MapMember);
        }

        public async Task<MembroEquipeDto> AddMemberAsync(Guid teamId, SalvarMembroDto dto)
        {
            await EnsureTeamAsync(teamId);
            var member = new TeamUser { TeamId = teamId };
            await ApplyMemberAsync(member, dto, teamId, null);
            _db.TeamUsers.Add(member);
            await _db.SaveChangesAsync();
            return MapMember(member);
        }

        public async Task<MembroEquipeDto> UpdateMemberAsync(Guid teamId, Guid memberId, SalvarMembroDto dto)
        {
            var member = await _db.TeamUsers.SingleOrDefaultAsync(m => m.Id == memberId && m.TeamId == teamId)
                ?? throw Messages.NotFound("Membro não encontrado.");
            await ApplyMemberAsync(member, dto, teamId, memberId);
            member.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return MapMember(member);
        }

        public async Task RemoveMemberAsync(Guid teamId, Guid memberId)
        {
            var member = await _db.TeamUsers.SingleOrDefaultAsync(m => m.Id == memberId && m.TeamId == teamId)
                ?? throw Messages.NotFound("Membro não encontrado.");
            _db.TeamUsers.Remove(member);
            await _db.SaveChangesAsync();
        }

        public async Task<CapacityResult> GetCapacityAsync(Guid teamId, string? mes)
        {
            var team = await _db.Teams.AsNoTracking().Include(t => t.Members).SingleOrDefaultAsync(t => t.Id == teamId)
                ?? throw Messages.NotFound("Equipe não encontrada.");
            var (year, month) = ParseMonth(mes);

            // Exceção do UC13: equipe sem membros -> disponivel = false (RN18) — tratado no calculador.
            return await _loader.CapacityAsync(team, year, month);
        }

        private static (int Year, int Month) ParseMonth(string? mes)
        {
            if (string.IsNullOrWhiteSpace(mes))
            {
                var now = DateTime.UtcNow;
                return (now.Year, now.Month);
            }

            if (DateTime.TryParseExact(mes, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return (parsed.Year, parsed.Month);
            }

            throw new BusinessRuleException(400, "Mês inválido. Use o formato AAAA-MM.");
        }

        private async Task EnsureTeamAsync(Guid teamId)
        {
            if (!await _db.Teams.AnyAsync(t => t.Id == teamId))
            {
                throw Messages.NotFound("Equipe não encontrada.");
            }
        }

        private async Task<string> ValidateTeamAsync(SalvarEquipeDto dto, Guid? currentId)
        {
            var nome = dto.Nome?.Trim() ?? string.Empty;
            if (nome.Length == 0)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios,
                    new Dictionary<string, string[]> { ["nome"] = new[] { "O nome da equipe é obrigatório." } });
            }

            nome = nome.Length > 100 ? nome[..100] : nome;
            var lowered = nome.ToLower();
            if (await _db.Teams.AnyAsync(t => t.Id != currentId && t.Name.ToLower() == lowered))
            {
                throw new BusinessRuleException(409, "Já existe uma equipe com este nome.");
            }

            if (dto.PortfolioId.HasValue && !await _db.Portfolios.AnyAsync(p => p.Id == dto.PortfolioId))
            {
                throw Messages.NotFound("Portfólio não encontrado.");
            }

            return nome;
        }

        private async Task ApplyMemberAsync(TeamUser member, SalvarMembroDto dto, Guid teamId, Guid? currentId)
        {
            var errors = new Dictionary<string, string[]>();
            var nome = dto.Nome?.Trim() ?? string.Empty;
            var email = dto.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            if (nome.Length == 0) errors["nome"] = new[] { "O nome é obrigatório." };
            if (email.Length == 0) errors["email"] = new[] { "O e-mail é obrigatório." };
            if (dto.CustoHora is null) errors["custoHora"] = new[] { "O custo por hora é obrigatório." };
            if (dto.CapacidadeMensalHoras is null) errors["capacidadeMensalHoras"] = new[] { "A capacidade mensal é obrigatória." };
            if (errors.Count > 0)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios, errors);
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new BusinessRuleException(400, "Informe um e-mail válido.");
            }

            if (dto.CustoHora is < 0 or > Limites.CustoHoraMaximo)
            {
                throw new BusinessRuleException(400, "O custo por hora deve ficar entre 0 e R$ 100.000,00.");
            }

            if (dto.CapacidadeMensalHoras is < 1 or > 300)
            {
                throw new BusinessRuleException(400, "A capacidade mensal deve ficar entre 1 e 300 horas.");
            }

            if (await _db.TeamUsers.AnyAsync(m => m.TeamId == teamId && m.Id != currentId && m.Email == email))
            {
                throw new BusinessRuleException(409, "Este e-mail já é membro da equipe.");
            }

            if (dto.UsuarioId.HasValue && !await _db.Users.AnyAsync(u => u.Id == dto.UsuarioId))
            {
                throw Messages.NotFound("Usuário não encontrado.");
            }

            member.Name = nome.Length > 150 ? nome[..150] : nome;
            member.Email = email;
            member.UserId = dto.UsuarioId;
            member.HourlyCost = Math.Round(dto.CustoHora!.Value, 2);
            member.MonthlyCapacityHours = dto.CapacidadeMensalHoras!.Value;
        }

        // Mesmos perfis da policy EditarEquipes.
        private bool CanSeeInviteCode() =>
            _currentUser.IsInRole(RoleName.Administrador) || _currentUser.IsInRole(RoleName.TechLead);

        private static EquipeDto Map(Team team, bool showInviteCode) => new()
        {
            Id = team.Id,
            Nome = team.Name,
            PortfolioId = team.PortfolioId,
            PortfolioNome = team.Portfolio?.Name,
            CapacidadeMensalTotal = team.Members.Sum(m => m.MonthlyCapacityHours),
            CodigoConvite = showInviteCode ? team.InviteCode : null,
            Membros = team.Members.OrderBy(m => m.Name).Select(MapMember).ToList(),
        };

        private static MembroEquipeDto MapMember(TeamUser m) => new()
        {
            Id = m.Id,
            EquipeId = m.TeamId,
            UsuarioId = m.UserId,
            Nome = m.Name,
            Email = m.Email,
            CustoHora = m.HourlyCost,
            CapacidadeMensalHoras = m.MonthlyCapacityHours,
        };
    }
}
