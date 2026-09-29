using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Role;
using Prumo.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Lists all available roles (Desenvolvedor, QA, ProductOwner, TechLead, GerenteProjeto, Diretoria, Administrador).
        /// Used by the front-end to populate the profile selector when creating/editing users.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
        {
            var roles = await _roleService.GetAllAsync();
            return Ok(roles);
        }
    }
}
