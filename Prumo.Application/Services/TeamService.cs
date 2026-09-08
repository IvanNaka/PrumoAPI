using Prumo.Application.DTOs.Team;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Prumo.Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPortfolioRepository _portfolioRepository;

        public TeamService(
            ITeamRepository teamRepository,
            IUserRepository userRepository,
            IPortfolioRepository portfolioRepository)
        {
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _portfolioRepository = portfolioRepository;
        }

        public async Task<TeamDto?> GetByIdAsync(Guid id)
        {
            var team = await _teamRepository.GetByIdAsync(id);
            return team == null ? null : MapToDto(team);
        }

        public async Task<IEnumerable<TeamDto>> GetAllAsync()
        {
            var teams = await _teamRepository.GetAllAsync();
            return teams.Select(MapToDto);
        }

        public async Task<IEnumerable<TeamDto>> GetByPortfolioIdAsync(Guid portfolioId)
        {
            var teams = await _teamRepository.GetByPortfolioIdAsync(portfolioId);
            return teams.Select(MapToDto);
        }

        public async Task<TeamDto> CreateAsync(CreateTeamDto dto)
        {
            var portfolioExists = await _portfolioRepository.ExistsAsync(dto.PortfolioId);
            if (!portfolioExists)
            {
                throw new InvalidOperationException("Portfolio not found.");
            }

            if (dto.OwnerUserId.HasValue)
            {
                var ownerExists = await _userRepository.ExistsAsync(dto.OwnerUserId.Value);
                if (!ownerExists)
                {
                    throw new InvalidOperationException("Owner user not found.");
                }
            }

            var team = new Team
            {
                PortfolioId = dto.PortfolioId,
                Name = dto.Name,
                OwnerUserId = dto.OwnerUserId,
                InviteCode = await GenerateUniqueInviteCodeAsync()
            };

            var created = await _teamRepository.AddAsync(team);
            var createdWithIncludes = await _teamRepository.GetByIdAsync(created.Id);

            return MapToDto(createdWithIncludes ?? created);
        }

        public async Task UpdateAsync(UpdateTeamDto dto)
        {
            var existing = await _teamRepository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            if (dto.OwnerUserId.HasValue)
            {
                var ownerExists = await _userRepository.ExistsAsync(dto.OwnerUserId.Value);
                if (!ownerExists)
                {
                    throw new InvalidOperationException("Owner user not found.");
                }
            }

            existing.Name = dto.Name;
            existing.OwnerUserId = dto.OwnerUserId;
            existing.UpdatedDate = DateTime.UtcNow;

            await _teamRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _teamRepository.DeleteAsync(id);
        }

        public async Task<TeamDto?> AddMemberAsync(Guid teamId, AddTeamMemberDto dto)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            var userExists = await _userRepository.ExistsAsync(dto.UserId);
            if (!userExists)
            {
                throw new InvalidOperationException("User not found.");
            }

            var existingMembership = await _teamRepository.GetMembershipAsync(teamId, dto.UserId);
            if (existingMembership != null)
            {
                throw new InvalidOperationException("User is already a member of this team.");
            }

            var teamUser = new TeamUser
            {
                TeamId = teamId,
                UserId = dto.UserId
            };

            await _teamRepository.AddMemberAsync(teamUser);

            var updatedTeam = await _teamRepository.GetByIdAsync(teamId);
            return updatedTeam == null ? null : MapToDto(updatedTeam);
        }

        public async Task<TeamDto?> RemoveMemberAsync(Guid teamId, Guid userId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            var membership = await _teamRepository.GetMembershipAsync(teamId, userId);
            if (membership == null)
            {
                throw new InvalidOperationException("User is not a member of this team.");
            }

            await _teamRepository.RemoveMemberAsync(membership);

            var updatedTeam = await _teamRepository.GetByIdAsync(teamId);
            return updatedTeam == null ? null : MapToDto(updatedTeam);
        }

        public async Task<TeamDto> JoinAsync(Guid userId, string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                throw new KeyNotFoundException("Invalid invite code.");
            }

            var team = await _teamRepository.GetByInviteCodeAsync(inviteCode);
            if (team == null)
            {
                throw new KeyNotFoundException("Invalid invite code.");
            }

            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists)
            {
                throw new InvalidOperationException("User not found.");
            }

            var existingMembership = await _teamRepository.GetMembershipAsync(team.Id, userId);
            if (existingMembership != null)
            {
                throw new InvalidOperationException("User is already a member of this team.");
            }

            var teamUser = new TeamUser
            {
                TeamId = team.Id,
                UserId = userId
            };

            await _teamRepository.AddMemberAsync(teamUser);

            var updatedTeam = await _teamRepository.GetByIdAsync(team.Id);
            return MapToDto(updatedTeam ?? team);
        }

        public async Task<string?> GetInviteCodeAsync(Guid teamId)
        {
            var team = await _teamRepository.GetByIdAsync(teamId);
            return team?.InviteCode;
        }

        private async Task<string> GenerateUniqueInviteCodeAsync()
        {
            const int maxAttempts = 10;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var code = GenerateInviteCode();
                if (!await _teamRepository.InviteCodeExistsAsync(code))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Failed to generate a unique team invite code. Please try again.");
        }

        // Unambiguous uppercase alphanumeric charset (no 0/O or 1/I) to keep the code easy to
        // read aloud and type in manually when sharing it with teammates.
        private const string InviteCodeCharset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int InviteCodeLength = 8;

        private static string GenerateInviteCode()
        {
            Span<byte> randomBytes = stackalloc byte[InviteCodeLength];
            RandomNumberGenerator.Fill(randomBytes);

            var chars = new char[InviteCodeLength];
            for (var i = 0; i < InviteCodeLength; i++)
            {
                chars[i] = InviteCodeCharset[randomBytes[i] % InviteCodeCharset.Length];
            }

            return new string(chars);
        }

        private static TeamDto MapToDto(Team team)
        {
            return new TeamDto
            {
                Id = team.Id,
                PortfolioId = team.PortfolioId,
                PortfolioName = team.Portfolio?.Name,
                Name = team.Name,
                InviteCode = team.InviteCode,
                OwnerUserId = team.OwnerUserId,
                OwnerUserName = team.OwnerUser?.Name,
                CreatedDate = team.CreatedDate,
                Members = team.Members?.Select(m => new TeamMemberDto
                {
                    UserId = m.UserId,
                    UserName = m.User?.Name ?? string.Empty,
                    UserEmail = m.User?.Email ?? string.Empty,
                    AddedDate = m.CreatedDate
                }).ToList() ?? new List<TeamMemberDto>()
            };
        }
    }
}
