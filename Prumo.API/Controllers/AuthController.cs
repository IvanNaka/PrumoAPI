using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Auth;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>UC1: troca o ID token do Google pelo JWT do Prumo.</summary>
        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<ActionResult<LoginResponseDto>> LoginGoogle([FromBody] GoogleLoginDto dto)
        {
            return Ok(await _authService.LoginGoogleAsync(dto?.IdToken ?? string.Empty));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<AuthUserDto>> Me()
        {
            return Ok(await _authService.GetCurrentUserAsync());
        }
    }
}
