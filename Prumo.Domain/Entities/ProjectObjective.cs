using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Entities
{
    public class ProjectObjective : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public Guid ObjectiveId { get; set; }
    }
}
