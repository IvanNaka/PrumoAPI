using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Authorization;
using Prumo.API.Security;
using Prumo.Application.DTOs.Auth;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public AuthController(IAuthService authService, IConfiguration configuration, IHostEnvironment environment)
        {
            _authService = authService;
            _configuration = configuration;
            _environment = environment;
        }

        /// <summary>UC1: troca o ID token do Google pelo JWT do Prumo.</summary>
        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<ActionResult<LoginResponseDto>> LoginGoogle([FromBody] GoogleLoginDto dto)
        {
            return Ok(await _authService.LoginGoogleAsync(dto?.IdToken ?? string.Empty));
        }

        /// <summary>
        /// Login de teste (seção 4.9 b): JWT de uma conta simulada já cadastrada. Só existe no ambiente
        /// de testes; fora dele responde 404 (ver <see cref="AuthTeste"/>).
        /// </summary>
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("teste")]
        public async Task<ActionResult<LoginResponseDto>> LoginTeste(
            [FromBody] TesteLoginDto dto,
            [FromHeader(Name = AuthTeste.Cabecalho)] string? chave)
        {
            if (!AuthTeste.Habilitado(_configuration, _environment) || !AuthTeste.ChaveValida(_configuration, chave))
            {
                return NotFound();
            }

            return Ok(await _authService.LoginTesteAsync(dto?.Email ?? string.Empty));
        }

        [Authorize(Policy = Policies.Autenticado)]
        [HttpGet("me")]
        public async Task<ActionResult<AuthUserDto>> Me()
        {
            return Ok(await _authService.GetCurrentUserAsync());
        }
    }
}
