using System;

namespace Prumo.Application.DTOs.Integration
{
    /// <summary>
    /// Options passed to a provider when fetching data, allowing incremental syncs
    /// (only items changed since the last successful sync).
    /// </summary>
    public class IntegrationSyncOptions
    {
        public DateTime? SinceUtc { get; set; }
    }
}
