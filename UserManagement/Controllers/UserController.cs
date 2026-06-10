using Microsoft.AspNetCore.Mvc;
using UserManagement.Domain;
using UserManagement.DTO;
using Microsoft.AspNetCore.Authorization;
using UserManagement.Services;
using UserManagement.Infrastructure;
namespace UserManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        EmailService _emailservice;
        public UserController(UserService userService, EmailService emailservice)
        {
            _userService = userService;
            _emailservice = emailservice;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> RegisterUser([FromBody] RegisterUserDTO userDTO)
        {
            var result = await _userService.RegisterAsync(userDTO);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<string>>> Login([FromBody] LoginUserDTO loginDTO)
        {
            var result = await _userService.LoginAsync(loginDTO);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        [Authorize]
        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<GetUserDTO>>>> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }
        [Authorize]
        [HttpPost("block")]
        public async Task<ActionResult<ApiResponse<string>>> BlockUsers([FromBody] List<Guid> userIds)
        {
            var result = await _userService.ChangeUsersStatusAsync(userIds, ModerationAction.Block);
            return Ok(result);
        }
        [Authorize]
        [HttpPost("unblock")]
        public async Task<ActionResult<ApiResponse<string>>> UnblockUsers([FromBody] List<Guid> userIds)
        {
            var result = await _userService.ChangeUsersStatusAsync(userIds, ModerationAction.Unblock);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("delete")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUsers([FromBody] List<Guid> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return BadRequest(ApiResponse<string>.Fail("No user IDs provided."));
            }

            var result = await _userService.DeleteUsersAsync(userIds);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("delete-unverified")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUnverifiedUsers()
        {
            var result = await _userService.DeleteUnverifiedUsersAsync();
            return Ok(result);
        }
        [Authorize]
        [HttpPost("send-verification")]
        public async Task<ActionResult<ApiResponse<string>>> SendVerificationMessage(string clientEmail)
        {
            var result = await _userService.SendVerificationCodeAsync(clientEmail);
            return Ok(result);
        }
        [Authorize]
        [HttpPost("verify")]
        public async Task<ActionResult<ApiResponse>> VerifyCode(string clientEmail, string code)
        {
            var result = await _userService.VerifyCodeAsync(clientEmail, code);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }
    }
}
