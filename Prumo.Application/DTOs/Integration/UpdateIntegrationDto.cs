namespace Prumo.Application.DTOs.Integration
{
    public class UpdateIntegrationDto
    {
        public string ApiUrl { get; set; }

        /// <summary>
        /// Optional: only sent when the credential needs to be rotated (e.g. after a token expires).
        /// </summary>
        public string Token { get; set; }

        public bool IsActive { get; set; }

        public int SyncIntervalMinutes { get; set; } = 60;
    }
}
