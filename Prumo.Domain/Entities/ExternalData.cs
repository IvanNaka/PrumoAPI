using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Entities
{
    public class ExternalData : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IntegrationId { get; set; } = Guid.Empty;

        public string ExternalId { get; set; }
        public string RawDataJson { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.Now;
    }
}
