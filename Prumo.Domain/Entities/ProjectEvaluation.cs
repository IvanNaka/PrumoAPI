using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Entities
{
    public class ProjectEvaluation : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PriorityCriteriaId { get; set; }
        public PriorityCriteria PriorityCriteria{ get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public Guid ProjectId { get; set; }
        public Project Project { get; set; }        
        public decimal Value { get; set; } = 0;
    }
}
