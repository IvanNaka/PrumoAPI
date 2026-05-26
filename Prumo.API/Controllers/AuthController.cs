using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Auth;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("google")]
        public async Task<IActionResult> Google([FromBody] GoogleLoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.IdToken))
                return BadRequest("idToken is required.");

            var jwt = await _authService.SignInWithGoogleAsync(dto.IdToken);
            return Ok(new { token = jwt });
        }
    }
}