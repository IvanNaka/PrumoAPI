using Prumo.Domain.Enums;
using System;

namespace Prumo.Application.DTOs.Integration
{
    public class IntegrationDto
    {
        public Guid Id { get; set; }
        public IntegrationType Type { get; set; }
        public string ApiUrl { get; set; }
        public bool IsActive { get; set; }
        public int SyncIntervalMinutes { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public string LastSyncStatus { get; set; }
    }
}
