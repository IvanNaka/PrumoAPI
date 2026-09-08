using System;

namespace Prumo.Application.DTOs.User
{
    public class UpdateUserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid RoleId { get; set; }

        /// <summary>
        /// Optional. Only provide a value when the password should be changed.
        /// </summary>
        public string? Password { get; set; }
    }
}
