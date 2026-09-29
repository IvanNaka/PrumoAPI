using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.User;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // RF03 — gestão de usuários.
    [ApiController]
    [Authorize]
    [Route("api/usuarios")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            return Ok(await _userService.GetAllAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
        {
            var created = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            return Ok(await _userService.UpdateAsync(id, dto));
        }

        [HttpPatch("{id:guid}/ativo")]
        public async Task<IActionResult> SetActive(Guid id, [FromBody] SetUserActiveDto dto)
        {
            await _userService.SetActiveAsync(id, dto.Ativo);
            return NoContent();
        }
    }
}
