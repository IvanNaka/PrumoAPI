namespace Prumo.API.Authorization
{
    /// <summary>
    /// Centralizes role-group constants used in [Authorize(Roles = ...)] attributes,
    /// so role combinations stay consistent across controllers.
    /// Values must match Prumo.Domain.Enums.RoleName member names exactly.
    /// </summary>
    public static class RoleGroups
    {
        public const string Admin = "Administrador";

        /// <summary>
        /// Roles allowed to manage teams (create/edit/delete teams and manage members).
        /// </summary>
        public const string TeamManagement = "Administrador,TechLead";

        /// <summary>
        /// Roles allowed to manage user accounts (create/edit/delete users).
        /// </summary>
        public const string UserManagement = "Administrador";
    }
}
