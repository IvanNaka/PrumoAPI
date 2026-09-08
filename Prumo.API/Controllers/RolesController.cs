using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Role;
using Prumo.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.API.Controllers
{
    // NOTE: [Authorize] temporarily removed - all endpoints are open while role
    // permissions are disabled. Re-add [Authorize] / role checks when re-enabling.
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Lists all available roles (e.g. Admin, PO, Gerente, Diretoria, TechLead, ScrumMaster, QA, DEV).
        /// Used by the front-end to populate the RoleId selector when creating/editing users.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
        {
            var roles = await _roleService.GetAllAsync();
            return Ok(roles);
        }
    }
}
