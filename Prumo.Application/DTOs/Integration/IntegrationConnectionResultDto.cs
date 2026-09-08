namespace Prumo.Application.DTOs.Integration
{
    /// <summary>
    /// Result of validating the connection/credentials before saving a new integration (UC15).
    /// </summary>
    public class IntegrationConnectionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
