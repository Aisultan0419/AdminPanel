using Microsoft.AspNetCore.Mvc;
using UserManagement.Domain;
using UserManagement.DTO;
using UserManagement.Services;
namespace UserManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<object>>> RegisterUser([FromBody] RegisterUserDTO userDTO)
        {
            var result = await _userService.RegisterAsync(userDTO);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
