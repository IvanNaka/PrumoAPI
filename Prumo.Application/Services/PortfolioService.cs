using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Portfolio;
using Prumo.Application.Interfaces;
using Prumo.Application.StateMachines;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // RF04, RF05, UC2, UC3 e ciclo de vida da Figura 27.
    public class PortfolioService : IPortfolioService
    {
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;
        private readonly ICurrentUserService _currentUser;

        public PortfolioService(IAppDbContext db, IPortfolioAccessService access, ICurrentUserService currentUser)
        {
            _db = db;
            _access = access;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<PortfolioDto>> GetVisibleAsync()
        {
            var query = _db.Portfolios.AsNoTracking();

            // D11: o usuário vê os portfólios em que é responsável ou membro; Administrador e Diretoria veem todos.
            if (!_access.CanSeeAll)
            {
                var userId = _currentUser.RequireUserId();
                query = query.Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId));
            }

            var portfolios = await Project(query).ToListAsync();
            return portfolios
                .OrderBy(p => p.Status == nameof(PortfolioStatus.Encerrado))
                .ThenBy(p => p.Nome);
        }

        public async Task<PortfolioDto?> GetByIdAsync(Guid id)
        {
            await _access.EnsureAccessAsync(id);
            return await Project(_db.Portfolios.AsNoTracking().Where(p => p.Id == id)).SingleOrDefaultAsync();
        }

        public async Task<PortfolioDto> CreateAsync(CreatePortfolioDto dto)
        {
            var (nome, responsavelId) = await ValidateAsync(dto, null);

            var portfolio = new Portfolio
            {
                Name = nome,
                Description = Clean(dto.Descricao),
                Goal = Clean(dto.Objetivo),
                OwnerId = responsavelId,
                Status = PortfolioStatus.Criado,
            };

            // O responsável é incluído automaticamente como membro. Quem cadastrou também passa a
            // ser membro, para não perder o acesso ao portfólio que acabou de criar.
            portfolio.Members.Add(new PortfolioMember { PortfolioId = portfolio.Id, UserId = responsavelId });
            var creatorId = _currentUser.RequireUserId();
            if (creatorId != responsavelId)
            {
                portfolio.Members.Add(new PortfolioMember { PortfolioId = portfolio.Id, UserId = creatorId });
            }

            _db.Portfolios.Add(portfolio);
            await _db.SaveChangesAsync();
            return (await GetByIdAsync(portfolio.Id))!;
        }

        public async Task<PortfolioDto> UpdateAsync(Guid id, CreatePortfolioDto dto)
        {
            await _access.EnsureWriteAccessAsync(id);
            var portfolio = await _db.Portfolios.Include(p => p.Members).SingleAsync(p => p.Id == id);
            var (nome, responsavelId) = await ValidateAsync(dto, id);

            portfolio.Name = nome;
            portfolio.Description = Clean(dto.Descricao);
            portfolio.Goal = Clean(dto.Objetivo);
            portfolio.OwnerId = responsavelId;
            portfolio.UpdatedDate = DateTime.UtcNow;
            if (portfolio.Members.All(m => m.UserId != responsavelId))
            {
                portfolio.Members.Add(new PortfolioMember { PortfolioId = id, UserId = responsavelId });
            }

            await _db.SaveChangesAsync();
            return (await GetByIdAsync(id))!;
        }

        public async Task<PortfolioDto> ApplyActionAsync(Guid id, string acao)
        {
            await _access.EnsureWriteAccessAsync(id);
            var portfolio = await _db.Portfolios.SingleAsync(p => p.Id == id);
            var evento = PortfolioStateMachine.AcaoDaRota(acao)
                ?? throw new BusinessRuleException(409, Messages.RN22_Transicao(portfolio.Status, acao));

            portfolio.Status = PortfolioStateMachine.Aplicar(portfolio.Status, evento);
            portfolio.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (await GetByIdAsync(id))!;
        }

        public async Task ApplyAutomaticEventAsync(Guid id, string evento)
        {
            var portfolio = await _db.Portfolios.SingleAsync(p => p.Id == id);
            var novo = PortfolioStateMachine.TentarAplicar(portfolio.Status, evento);

            // Configurado -> EmAnalise quando já há projetos cadastrados (ex.: projetos criados
            // antes do primeiro critério).
            if (novo == PortfolioStatus.Configurado && await _db.Projects.AnyAsync(p => p.PortfolioId == id))
            {
                novo = PortfolioStateMachine.TentarAplicar(novo, PortfolioStateMachine.PrimeiroProjeto);
            }

            if (novo != portfolio.Status)
            {
                portfolio.Status = novo;
                portfolio.UpdatedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<PortfolioMemberDto>> GetMembersAsync(Guid id)
        {
            await _access.EnsureAccessAsync(id);
            var ownerId = await _db.Portfolios.Where(p => p.Id == id).Select(p => p.OwnerId).SingleAsync();
            var members = await _db.PortfolioMembers.AsNoTracking()
                .Where(m => m.PortfolioId == id)
                .Select(m => new { m.User.Id, m.User.Name, m.User.Email, Roles = m.User.Roles.Select(r => r.Role).ToList() })
                .ToListAsync();

            return members
                .Select(m => new PortfolioMemberDto
                {
                    UsuarioId = m.Id,
                    Nome = m.Name,
                    Email = m.Email,
                    Perfis = m.Roles.Select(r => r.ToString()).OrderBy(r => r).ToList(),
                    Responsavel = m.Id == ownerId,
                })
                .OrderByDescending(m => m.Responsavel)
                .ThenBy(m => m.Nome)
                .ToList();
        }

        public async Task<IEnumerable<PortfolioMemberDto>> AddMemberAsync(Guid id, Guid userId)
        {
            await _access.EnsureOwnerOrAdminAsync(id);
            await _access.EnsureNotClosedAsync(id);

            var userExists = await _db.Users.AnyAsync(u => u.Id == userId && u.IsActive);
            if (!userExists)
            {
                throw Messages.NotFound("Usuário não encontrado.");
            }

            if (!await _db.PortfolioMembers.AnyAsync(m => m.PortfolioId == id && m.UserId == userId))
            {
                _db.PortfolioMembers.Add(new PortfolioMember { PortfolioId = id, UserId = userId });
                await _db.SaveChangesAsync();
            }

            return await GetMembersAsync(id);
        }

        public async Task RemoveMemberAsync(Guid id, Guid userId)
        {
            await _access.EnsureOwnerOrAdminAsync(id);
            await _access.EnsureNotClosedAsync(id);

            var ownerId = await _db.Portfolios.Where(p => p.Id == id).Select(p => p.OwnerId).SingleAsync();
            if (ownerId == userId)
            {
                throw new BusinessRuleException(409, "O responsável não pode ser removido do portfólio.");
            }

            var member = await _db.PortfolioMembers.SingleOrDefaultAsync(m => m.PortfolioId == id && m.UserId == userId);
            if (member != null)
            {
                _db.PortfolioMembers.Remove(member);
                await _db.SaveChangesAsync();
            }
        }

        private async Task<(string Nome, Guid ResponsavelId)> ValidateAsync(CreatePortfolioDto dto, Guid? currentId)
        {
            var errors = new Dictionary<string, string[]>();
            var nome = dto.Nome?.Trim() ?? string.Empty;
            if (nome.Length == 0)
            {
                errors["nome"] = new[] { "O nome é obrigatório." };
            }

            if (dto.ResponsavelId is null || dto.ResponsavelId == Guid.Empty)
            {
                errors["responsavelId"] = new[] { "O responsável é obrigatório." };
            }

            if (errors.Count > 0)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios, errors);
            }

            if (!await _db.Users.AnyAsync(u => u.Id == dto.ResponsavelId && u.IsActive))
            {
                throw Messages.NotFound("Responsável não encontrado.");
            }

            // Nome único entre os portfólios não encerrados.
            var lowered = nome.ToLower();
            var duplicated = await _db.Portfolios.AnyAsync(p =>
                p.Id != currentId && p.Status != PortfolioStatus.Encerrado && p.Name.ToLower() == lowered);
            if (duplicated)
            {
                throw new BusinessRuleException(409, "Já existe um portfólio com este nome.");
            }

            return (nome, dto.ResponsavelId!.Value);
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private IQueryable<PortfolioDto> Project(IQueryable<Portfolio> query) =>
            query.Select(p => new PortfolioDto
            {
                Id = p.Id,
                Nome = p.Name,
                Descricao = p.Description,
                Objetivo = p.Goal,
                ResponsavelId = p.OwnerId,
                ResponsavelNome = p.Owner.Name,
                Status = p.Status.ToString(),
                DataCriacao = p.CreatedDate,
                QuantidadeProjetos = _db.Projects.Count(pr => pr.PortfolioId == p.Id),
                QuantidadeCriterios = _db.PriorityCriterias.Count(c => c.PortfolioId == p.Id),
                QuantidadeMembros = p.Members.Count,
            });
    }
}
