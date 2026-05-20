using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Entities
{
    public class ProjectMetric : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get ; set; }

        public decimal Value { get; set; }
        public decimal Effort { get; set; }
        public decimal Risk { get; set; }

        public DateTime CollectedAt { get; set; } = DateTime.Now;   
    }
}
