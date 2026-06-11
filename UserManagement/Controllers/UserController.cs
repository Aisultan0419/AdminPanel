using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Domain;
using UserManagement.DTO;
using UserManagement.Services;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController(UserService userService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDTO dto)
        {
            var result = await userService.RegisterAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDTO dto)
        {
            var result = await userService.LoginAsync(dto);
            if (!result.Success) return Unauthorized(result);
            return Ok(result);
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await userService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpPost("block")]
        [Authorize]
        public async Task<IActionResult> BlockUsers([FromBody] List<Guid> UserIds)
        {
            var result = await userService.ChangeUsersStatusAsync(UserIds, ModerationAction.Block);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("unblock")]
        [Authorize]
        public async Task<IActionResult> UnblockUsers([FromBody] List<Guid> UserIds)
        {
            var result = await userService.ChangeUsersStatusAsync(UserIds, ModerationAction.Unblock);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("delete")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUsers([FromBody] List<Guid> userIds)
        {
            if (userIds == null || userIds.Count == 0)
            {
                return BadRequest(ApiResponse<string>.Fail("No user IDs provided."));
            }

            var result = await userService.DeleteUsersAsync(userIds);
            return Ok(result);
        }

        [HttpPost("delete-unverified")]
        [Authorize]
        public async Task<IActionResult> DeleteUnverifiedUsers()
        {
            var result = await userService.DeleteUnverifiedUsersAsync();
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("send-verification")]
        public async Task<ActionResult<ApiResponse<string>>> SendVerificationMessage(string clientEmail)
        {
            var result = await userService.SendVerificationCodeAsync(clientEmail);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("verify")]
        public async Task<ActionResult<ApiResponse>> VerifyCode(string clientEmail, string code)
        {
            var result = await userService.VerifyCodeAsync(clientEmail, code);
            if (result.Success)
            {
                return Ok(result.Message);
            }
            return BadRequest(result.Message);
        }
    }
}
