using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prumo.Application.DTOs.TeamCapacity;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Prumo.Application.Services
{
    public class TeamCapacityService : ITeamCapacityService
    {
        private readonly ITeamCapacityRepository _capacityRepository;
        private readonly ITeamRepository _teamRepository;

        public TeamCapacityService(
            ITeamCapacityRepository capacityRepository,
            ITeamRepository teamRepository)
        {
            _capacityRepository = capacityRepository;
            _teamRepository = teamRepository;
        }

        public async Task<TeamCapacityEntryDto> GetByIdAsync(Guid id)
        {
            var entry = await _capacityRepository.GetByIdAsync(id);
            return entry == null ? null : MapToDto(entry);
        }

        public async Task<IEnumerable<TeamCapacityEntryDto>> GetByTeamIdAsync(Guid teamId)
        {
            var entries = await _capacityRepository.GetByTeamIdAsync(teamId);
            return entries.Select(MapToDto);
        }

        public async Task<(TeamCapacityEntryDto Entry, bool Created)> UpsertAsync(CreateTeamCapacityDto dto)
        {
            if (dto.AvailableHours <= 0)
            {
                throw new ArgumentException("availableHours deve ser maior que zero.");
            }

            if (dto.Month < 1 || dto.Month > 12)
            {
                throw new ArgumentException("month deve estar entre 1 e 12.");
            }

            var membership = await _teamRepository.GetMembershipAsync(dto.TeamId, dto.UserId);
            if (membership == null)
            {
                throw new ArgumentException("userId não é membro do teamId informado.");
            }

            var existing = await _capacityRepository.GetByTeamUserYearMonthAsync(
                dto.TeamId, dto.UserId, dto.Year, dto.Month);

            if (existing != null)
            {
                existing.AvailableHours = dto.AvailableHours;
                existing.OccupancyPercent = CalculateOccupancyPercent(existing.AllocatedHours, existing.AvailableHours);
                existing.UpdatedDate = DateTime.UtcNow;

                await _capacityRepository.UpdateAsync(existing);

                return (MapToDto(existing), false);
            }

            var entry = new TeamCapacityEntry
            {
                TeamId = dto.TeamId,
                UserId = dto.UserId,
                Year = dto.Year,
                Month = dto.Month,
                AvailableHours = dto.AvailableHours,
                // O backend ainda não calcula alocação a partir de demandas/estimativas
                // (fonte de dados prevista, mas não disponível): retorna 0 por ora.
                AllocatedHours = 0,
            };
            entry.OccupancyPercent = CalculateOccupancyPercent(entry.AllocatedHours, entry.AvailableHours);

            await _capacityRepository.AddAsync(entry);
            var created = await _capacityRepository.GetByIdAsync(entry.Id);

            return (MapToDto(created ?? entry), true);
        }

        public async Task UpdateAsync(UpdateTeamCapacityDto dto)
        {
            var existing = await _capacityRepository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException("Registro de capacidade não encontrado.");
            }

            if (dto.AvailableHours <= 0)
            {
                throw new ArgumentException("availableHours deve ser maior que zero.");
            }

            existing.AvailableHours = dto.AvailableHours;
            existing.OccupancyPercent = CalculateOccupancyPercent(existing.AllocatedHours, existing.AvailableHours);
            existing.UpdatedDate = DateTime.UtcNow;

            await _capacityRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _capacityRepository.DeleteAsync(id);
        }

        private static int CalculateOccupancyPercent(decimal allocatedHours, decimal availableHours)
        {
            if (availableHours <= 0)
            {
                return 0;
            }

            return (int)Math.Round(allocatedHours / availableHours * 100, MidpointRounding.AwayFromZero);
        }

        private static TeamCapacityEntryDto MapToDto(TeamCapacityEntry entry)
        {
            return new TeamCapacityEntryDto
            {
                Id = entry.Id,
                TeamId = entry.TeamId,
                UserId = entry.UserId,
                UserName = entry.User?.Name,
                Year = entry.Year,
                Month = entry.Month,
                AvailableHours = entry.AvailableHours,
                AllocatedHours = entry.AllocatedHours,
                OccupancyPercent = entry.OccupancyPercent,
                UpdatedAt = entry.UpdatedDate ?? entry.CreatedDate,
            };
        }
    }
}
