using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;
using Plantonize.Plantao.Infrastructure;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class ProjectEvaluationRepository : Repository<ProjectEvaluation>, IProjectEvaluationRepository
    {
        public ProjectEvaluationRepository(PrumoDbContext context) : base(context)
        {
        }
    }
}