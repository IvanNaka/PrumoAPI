using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Entities
{
    public class CriteriaValue : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EvaluatorUserId { get; set; }

        public decimal Value { get; set; } = 0;
        public Guid PriorityCriteriaId { get; set; }
    }
}
