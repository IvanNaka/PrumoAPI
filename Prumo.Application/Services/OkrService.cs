using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Okr;
using Prumo.Application.Indicators;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;

namespace Prumo.Application.Services
{
    // OKRs e Key Results (RF14, RF15, RF17, UC8, F4).
    public class OkrService : IOkrService
    {
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;

        public OkrService(IAppDbContext db, IPortfolioAccessService access)
        {
            _db = db;
            _access = access;
        }

        public async Task<IEnumerable<OkrDto>> GetAllAsync()
        {
            var okrs = await _db.Objectives.AsNoTracking()
                .Include(o => o.KeyResults)
                .OrderBy(o => o.Title)
                .ToListAsync();
            var counts = await _db.ProjectObjectives.AsNoTracking()
                .GroupBy(po => po.ObjectiveId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);

            return okrs.Select(o => Map(o, counts.GetValueOrDefault(o.Id)));
        }

        public async Task<OkrDto> GetAsync(Guid id)
        {
            var okr = await _db.Objectives.AsNoTracking().Include(o => o.KeyResults).SingleOrDefaultAsync(o => o.Id == id)
                ?? throw Messages.NotFound(Messages.RN14_OkrNaoEncontrado);
            var count = await _db.ProjectObjectives.CountAsync(po => po.ObjectiveId == id);
            return Map(okr, count);
        }

        public async Task<OkrDto> CreateAsync(SalvarOkrDto dto)
        {
            Validate(dto);
            var okr = new Objective();
            ApplyHeader(okr, dto);
            foreach (var kr in dto.KeyResults!)
            {
                okr.KeyResults.Add(NewKeyResult(okr.Id, kr));
            }

            // Salva o OKR e os KRs numa única transação (SaveChanges é atômico).
            _db.Objectives.Add(okr);
            await _db.SaveChangesAsync();
            return await GetAsync(okr.Id);
        }

        public async Task<OkrDto> UpdateAsync(Guid id, SalvarOkrDto dto)
        {
            var okr = await _db.Objectives.Include(o => o.KeyResults).SingleOrDefaultAsync(o => o.Id == id)
                ?? throw Messages.NotFound(Messages.RN14_OkrNaoEncontrado);
            Validate(dto);
            ApplyHeader(okr, dto);
            okr.UpdatedDate = DateTime.UtcNow;

            var informados = dto.KeyResults!.Where(k => k.Id.HasValue).Select(k => k.Id!.Value).ToHashSet();
            foreach (var removido in okr.KeyResults.Where(k => !informados.Contains(k.Id)).ToList())
            {
                _db.KeyResults.Remove(removido);
            }

            foreach (var kr in dto.KeyResults!)
            {
                var existing = kr.Id.HasValue ? okr.KeyResults.SingleOrDefault(k => k.Id == kr.Id) : null;
                if (existing == null)
                {
                    _db.KeyResults.Add(NewKeyResult(okr.Id, kr));
                }
                else
                {
                    existing.Title = kr.Descricao!.Trim();
                    existing.TargetValue = kr.Meta!.Value;
                    existing.CurrentValue = kr.ValorAtual ?? 0;
                    existing.UpdatedDate = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return await GetAsync(id);
        }

        public async Task<KeyResultDto> UpdateKeyResultValueAsync(Guid keyResultId, decimal? valorAtual)
        {
            var kr = await _db.KeyResults.SingleOrDefaultAsync(k => k.Id == keyResultId)
                ?? throw Messages.NotFound("Key Result não encontrado.");

            if (valorAtual is null or < 0)
            {
                throw new BusinessRuleException(400, "O valor atual deve ser maior ou igual a 0.");
            }

            kr.CurrentValue = valorAtual.Value;
            kr.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return MapKr(kr);
        }

        public async Task<IEnumerable<OkrResumoDto>> GetProjectOkrsAsync(Guid projectId)
        {
            await _access.EnsureProjectAccessAsync(projectId);
            var okrs = await _db.Objectives.AsNoTracking()
                .Include(o => o.KeyResults)
                .Where(o => o.Projects.Any(po => po.ProjectId == projectId))
                .OrderBy(o => o.Title)
                .ToListAsync();
            return okrs.Select(o => new OkrResumoDto { Id = o.Id, Titulo = o.Title, Progresso = Map(o).Progresso });
        }

        public async Task LinkProjectAsync(Guid projectId, Guid okrId)
        {
            await _access.EnsureProjectAccessAsync(projectId, write: true);
            await EnsureOkrExistsAsync(okrId);
            if (!await _db.ProjectObjectives.AnyAsync(po => po.ProjectId == projectId && po.ObjectiveId == okrId))
            {
                _db.ProjectObjectives.Add(new ProjectObjective { ProjectId = projectId, ObjectiveId = okrId });
                await _db.SaveChangesAsync();
            }
        }

        public async Task UnlinkProjectAsync(Guid projectId, Guid okrId)
        {
            await _access.EnsureProjectAccessAsync(projectId, write: true);
            var link = await _db.ProjectObjectives.SingleOrDefaultAsync(po => po.ProjectId == projectId && po.ObjectiveId == okrId);
            if (link != null)
            {
                _db.ProjectObjectives.Remove(link);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<OkrDto>> GetPortfolioOkrsAsync(Guid portfolioId)
        {
            await _access.EnsureAccessAsync(portfolioId);
            var okrs = await _db.Objectives.AsNoTracking()
                .Include(o => o.KeyResults)
                .Where(o => _db.PortfolioObjectives.Any(po => po.PortfolioId == portfolioId && po.ObjectiveId == o.Id))
                .OrderBy(o => o.Title)
                .ToListAsync();
            return okrs.Select(o => Map(o));
        }

        public async Task LinkPortfolioAsync(Guid portfolioId, Guid okrId)
        {
            await _access.EnsureWriteAccessAsync(portfolioId);
            await EnsureOkrExistsAsync(okrId);
            if (!await _db.PortfolioObjectives.AnyAsync(po => po.PortfolioId == portfolioId && po.ObjectiveId == okrId))
            {
                _db.PortfolioObjectives.Add(new PortfolioObjective { PortfolioId = portfolioId, ObjectiveId = okrId });
                await _db.SaveChangesAsync();
            }
        }

        public async Task UnlinkPortfolioAsync(Guid portfolioId, Guid okrId)
        {
            await _access.EnsureWriteAccessAsync(portfolioId);
            var link = await _db.PortfolioObjectives.SingleOrDefaultAsync(po => po.PortfolioId == portfolioId && po.ObjectiveId == okrId);
            if (link != null)
            {
                _db.PortfolioObjectives.Remove(link);
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>RN14: OKR não encontrado ao associar.</summary>
        private async Task EnsureOkrExistsAsync(Guid okrId)
        {
            if (!await _db.Objectives.AnyAsync(o => o.Id == okrId))
            {
                throw Messages.NotFound(Messages.RN14_OkrNaoEncontrado);
            }
        }

        private static void Validate(SalvarOkrDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Titulo))
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios,
                    new Dictionary<string, string[]> { ["titulo"] = new[] { "O título é obrigatório." } });
            }

            // RN13: um objetivo precisa de pelo menos um Key Result.
            if (dto.KeyResults == null || dto.KeyResults.Count == 0)
            {
                throw new BusinessRuleException(400, Messages.RN13_OkrSemKr);
            }

            if (dto.KeyResults.Any(k => string.IsNullOrWhiteSpace(k.Descricao) || k.Meta is null))
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios);
            }

            if (dto.KeyResults.Any(k => k.Meta <= 0))
            {
                throw new BusinessRuleException(400, "A meta do Key Result deve ser maior que 0.");
            }

            if (dto.KeyResults.Any(k => k.ValorAtual < 0))
            {
                throw new BusinessRuleException(400, "O valor atual deve ser maior ou igual a 0.");
            }

            if (dto.DataInicio.HasValue && dto.DataFim.HasValue && dto.DataFim < dto.DataInicio)
            {
                throw new BusinessRuleException(400, Messages.RN12_DataFim);
            }
        }

        private static void ApplyHeader(Objective okr, SalvarOkrDto dto)
        {
            var titulo = dto.Titulo!.Trim();
            okr.Title = titulo.Length > 200 ? titulo[..200] : titulo;
            okr.Description = string.IsNullOrWhiteSpace(dto.Descricao) ? null : dto.Descricao.Trim();
            okr.StartDate = dto.DataInicio;
            okr.EndDate = dto.DataFim;
        }

        private static KeyResult NewKeyResult(Guid okrId, SalvarKeyResultDto kr)
        {
            var descricao = kr.Descricao!.Trim();
            return new KeyResult
            {
                ObjectiveId = okrId,
                Title = descricao.Length > 300 ? descricao[..300] : descricao,
                TargetValue = kr.Meta!.Value,
                CurrentValue = kr.ValorAtual ?? 0,
            };
        }

        public static OkrDto Map(Objective okr, int projectCount = 0)
        {
            var krs = okr.KeyResults.OrderBy(k => k.CreatedDate).Select(MapKr).ToList();
            return new OkrDto
            {
                Id = okr.Id,
                Titulo = okr.Title,
                Descricao = okr.Description,
                DataInicio = okr.StartDate,
                DataFim = okr.EndDate,
                KeyResults = krs,
                QuantidadeProjetos = projectCount,
                Progresso = OkrProgressCalculator.Objective(okr.KeyResults.Select(k => new KeyResultValue(k.TargetValue, k.CurrentValue)).ToList()),
            };
        }

        public static KeyResultDto MapKr(KeyResult kr) => new()
        {
            Id = kr.Id,
            OkrId = kr.ObjectiveId,
            Descricao = kr.Title,
            Meta = kr.TargetValue,
            ValorAtual = kr.CurrentValue,
            Progresso = OkrProgressCalculator.KeyResult(kr.TargetValue, kr.CurrentValue),
        };
    }
}
