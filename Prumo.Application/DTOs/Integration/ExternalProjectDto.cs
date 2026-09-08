namespace Prumo.Application.DTOs.Integration
{
    /// <summary>
    /// Normalized representation of a project/board from the external tool
    /// (Jira project, Azure DevOps project, GitHub repository, Trello board).
    /// </summary>
    public class ExternalProjectDto
    {
        public string ExternalId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
    }
}
