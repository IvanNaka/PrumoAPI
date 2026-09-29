using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.User;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    /// <summary>
    /// Usuários ativos (id, nome, e-mail) para preencher os campos "responsável" e "membros"
    /// (UC2, UC7). Disponível a qualquer usuário autenticado; não expõe perfis nem permite edição.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/usuarios/ativos")]
    public class UserOptionsController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserOptionsController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserOptionDto>>> Get()
        {
            return Ok(await _userService.GetActiveOptionsAsync());
        }
    }
}
