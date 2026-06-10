using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using StackExchange.Redis;
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
        private readonly EmailService _emailService;
        private readonly IDatabase _redisDb;

        public UserService(AppDbContext context, PasswordHasherService passwordHasher, JwtService jwtService, EmailService emailService, IConnectionMultiplexer redis)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
            _redisDb = redis.GetDatabase();
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

        public async Task<ApiResponse<string>> SendVerificationCodeAsync(string clientEmail)
        {
            var code = GenerateVerificationCode();
            await _emailService.SendVerificationEmailAsync(clientEmail, code);

            var redisKey = $"verification:{clientEmail}";
            await _redisDb.StringSetAsync(redisKey, code, TimeSpan.FromMinutes(5));

            return ApiResponse<string>.Ok(string.Empty, "Verification code sent successfully.");
        }

        private string GenerateVerificationCode()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }

        public async Task<ApiResponse> VerifyCodeAsync(string clientEmail, string code)
        {
            var redisKey = $"verification:{clientEmail}";
            var value = await _redisDb.StringGetAsync(redisKey);

            if (value.IsNullOrEmpty || value != code)
            {
                return ApiResponse.Fail("Verification code is invalid or expired");
            }

            await _redisDb.KeyDeleteAsync(redisKey);

            var user = await _context.Users.FirstOrDefaultAsync(a => a.Email == clientEmail);
            user!.Status = UserStatus.Active;
            await _context.SaveChangesAsync();
            return ApiResponse.Ok("User is successfully verified");
        }
    }
}