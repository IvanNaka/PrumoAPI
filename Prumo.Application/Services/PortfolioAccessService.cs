using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.Interfaces;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    public class PortfolioAccessService : IPortfolioAccessService
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public PortfolioAccessService(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public bool CanSeeAll =>
            _currentUser.IsInRole(RoleName.Administrador) || _currentUser.IsInRole(RoleName.Diretoria);

        public async Task EnsureAccessAsync(Guid portfolioId)
        {
            var portfolio = await _db.Portfolios.AsNoTracking()
                .Where(p => p.Id == portfolioId)
                .Select(p => new { p.OwnerId })
                .SingleOrDefaultAsync()
                ?? throw Messages.NotFound("Portfólio não encontrado.");

            if (CanSeeAll)
            {
                return;
            }

            var userId = _currentUser.RequireUserId();
            var isMember = portfolio.OwnerId == userId
                || await _db.PortfolioMembers.AnyAsync(m => m.PortfolioId == portfolioId && m.UserId == userId);

            if (!isMember)
            {
                throw new BusinessRuleException(403, Messages.RN06_NaoMembro);
            }
        }

        public async Task EnsureNotClosedAsync(Guid portfolioId)
        {
            var status = await _db.Portfolios.AsNoTracking()
                .Where(p => p.Id == portfolioId)
                .Select(p => (PortfolioStatus?)p.Status)
                .SingleOrDefaultAsync();

            if (status == PortfolioStatus.Encerrado)
            {
                throw new BusinessRuleException(409, Messages.RN23_PortfolioEncerrado);
            }
        }

        public async Task EnsureWriteAccessAsync(Guid portfolioId)
        {
            await EnsureAccessAsync(portfolioId);
            await EnsureNotClosedAsync(portfolioId);
        }

        public async Task<Guid> EnsureProjectAccessAsync(Guid projectId, bool write = false)
        {
            var portfolioId = await _db.Projects.AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => (Guid?)p.PortfolioId)
                .SingleOrDefaultAsync()
                ?? throw Messages.NotFound("Projeto não encontrado.");

            if (write)
            {
                await EnsureWriteAccessAsync(portfolioId);
            }
            else
            {
                await EnsureAccessAsync(portfolioId);
            }

            return portfolioId;
        }

        public async Task EnsureOwnerOrAdminAsync(Guid portfolioId)
        {
            var ownerId = await _db.Portfolios.AsNoTracking()
                .Where(p => p.Id == portfolioId)
                .Select(p => (Guid?)p.OwnerId)
                .SingleOrDefaultAsync()
                ?? throw Messages.NotFound("Portfólio não encontrado.");

            if (_currentUser.IsInRole(RoleName.Administrador))
            {
                return;
            }

            if (ownerId != _currentUser.RequireUserId())
            {
                throw new BusinessRuleException(403, Messages.RN27_PerfilSemPermissao);
            }
        }
    }
}
