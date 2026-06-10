using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserManagement.Domain;

namespace UserManagement.Infrastructure
{
    public class UserStatusFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _context;

        public UserStatusFilter(AppDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                await next();
                return;
            }

            var identity = context.HttpContext.User.Identity;
            if (identity != null && identity.IsAuthenticated)
            {
                var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userIdClaim, out Guid userId))
                {
                    var user = await _context.Users
                        .AsNoTracking()
                        .Select(u => new { u.Id, u.IsBlocked })
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user == null || user.IsBlocked)
                    {
                        context.Result = new UnauthorizedObjectResult(
                            ApiResponse<object>.Fail("Your account is suspended or deleted. Access denied.")
                        );
                        return;
                    }
                }
            }

            await next();
        }
    }
}