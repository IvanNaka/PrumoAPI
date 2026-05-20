using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Entities
{    public class KeyResult : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ObjectiveId { get; set; }

        public string Title { get; set; } = string.Empty;
        public decimal TargetValue { get; set; } = 0;
        public decimal CurrentValue { get; set; } = 0;
    }
}
