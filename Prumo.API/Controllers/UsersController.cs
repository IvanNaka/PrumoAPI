using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.User;
using Prumo.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.API.Controllers
{
    // NOTE: [Authorize] temporarily removed - all endpoints are open while role
    // permissions are disabled. Re-add [Authorize] / role checks when re-enabling.
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto createDto)
        {
            if (createDto == null)
            {
                return BadRequest("Invalid payload.");
            }

            if (string.IsNullOrWhiteSpace(createDto.Name) ||
                string.IsNullOrWhiteSpace(createDto.Email) ||
                string.IsNullOrWhiteSpace(createDto.Password))
            {
                return BadRequest("Name, email and password are required.");
            }

            try
            {
                var createdUser = await _userService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto updateDto)
        {
            if (updateDto == null || id != updateDto.Id)
            {
                return BadRequest("ID mismatch or invalid payload.");
            }

            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            try
            {
                await _userService.UpdateAsync(updateDto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            await _userService.DeleteAsync(id);
            return NoContent();
        }
    }
}
