using Prumo.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Entities
{
    public class Alert : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProjectId { get; set; }

        public string Message { get; set; }
        public AlertType Type { get; set; } // Warning, Critical

        public bool IsResolved { get; set; } = false;
    }
}
