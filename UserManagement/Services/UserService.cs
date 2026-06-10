using Microsoft.EntityFrameworkCore;
using UserManagement.Domain;
using UserManagement.DTO;
using UserManagement.Infrastructure;


namespace UserManagement.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasherService _passwordHasher;
        private readonly JwtService _jwtService;
        public UserService(AppDbContext context, PasswordHasherService passwordHasher, JwtService jwtService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        public async Task<ApiResponse<string>> RegisterAsync(RegisterUserDTO dto)
        {
            var user = new User
            {
                Name = dto.name,
                Email = dto.email,
                PasswordHash = _passwordHasher.HashPassword(dto.plainPassword),
                Status = UserStatus.Unverified,
                LastActivityTime = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok(user.Id.ToString(), "The user has been successfully registered.");
        }

        public async Task<ApiResponse<string>> LoginAsync(LoginUserDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.email);
            if (user == null)
            {
                return ApiResponse<string>.Fail("Invalid email or password.");
            }

            if (user.IsBlocked)
            {
                return ApiResponse<string>.Fail("Your account has been blocked. Access denied.");
            }

            bool isPasswordValid = _passwordHasher.VerifyPassword(dto.password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return ApiResponse<string>.Fail("Invalid email or password.");
            }

            user.LastActivityTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            string token = _jwtService.GenerateToken(user);

            return ApiResponse<string>.Ok(token, "Login successful.");
        }

        public async Task<ApiResponse<List<GetUserDTO>>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Select(u => new GetUserDTO(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Status.ToString(),
                    u.LastActivityTime,
                    u.IsBlocked
                ))
                .ToListAsync();

            return ApiResponse<List<GetUserDTO>>.Ok(users, "Users retrieved successfully.");
        }

        public async Task<ApiResponse<string>> ChangeUsersStatusAsync(List<Guid> userIds, ModerationAction action)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            if (!users.Any()) return ApiResponse<string>.Fail("No users found.");
            bool isBlocked = action == ModerationAction.Block;
            foreach (var user in users)
            {
                user.IsBlocked = isBlocked;
            }

            await _context.SaveChangesAsync();
            string result = isBlocked ? "Users successfully are blocked" : "Users successfully are unblocked";
            return ApiResponse<string>.Ok(string.Empty, result);
        }

        public async Task<ApiResponse<string>> DeleteUsersAsync(List<Guid> userIds)
        {
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            if (!users.Any())
            {
                return ApiResponse<string>.Fail("No matching users found.");
            }

            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok(string.Empty, $"Successfully deleted {users.Count} users.");
        }

        public async Task<ApiResponse<string>> DeleteUnverifiedUsersAsync()
        {
            var unverifiedUsers = await _context.Users
                .Where(u => u.Status == UserStatus.Unverified)
                .ToListAsync();

            if (!unverifiedUsers.Any())
            {
                return ApiResponse<string>.Fail("No unverified users found.");
            }

            _context.Users.RemoveRange(unverifiedUsers);
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok(string.Empty, $"Successfully deleted {unverifiedUsers.Count} unverified users.");
        }
    }
}