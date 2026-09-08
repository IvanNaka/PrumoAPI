using Prumo.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Prumo.Application.DTOs.Integration
{
    /// <summary>
    /// Input for UC15 "Configurar Integração": informs the tool, URL and credentials.
    /// The connection is validated before the configuration is persisted.
    /// </summary>
    public class ConfigureIntegrationDto
    {
        [Required]
        public IntegrationType Type { get; set; }

        [Required]
        public string ApiUrl { get; set; }

        [Required]
        public string Token { get; set; }

        /// <summary>
        /// Sync period in minutes for the automatic synchronization routine (RF51). Defaults to 60.
        /// </summary>
        public int SyncIntervalMinutes { get; set; } = 60;
    }
}
