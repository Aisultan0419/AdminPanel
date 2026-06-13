using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using StackExchange.Redis;
using UserManagement.Domain;
using UserManagement.DTO;
using UserManagement.Infrastructure;


namespace UserManagement.Services
{
    public class UserService(AppDbContext context, PasswordHasherService passwordHasher, JwtService jwtService, EmailService emailService, IConnectionMultiplexer redis)
    {
        private readonly AppDbContext _context = context;
        private readonly PasswordHasherService _passwordHasher = passwordHasher;
        private readonly JwtService _jwtService = jwtService;
        private readonly EmailService _emailService = emailService;
        private readonly IDatabase _redisDb = redis.GetDatabase();

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
                return ApiResponse<string>.Fail("User does not exists");
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

        public async Task<ApiResponse> ChangeUsersStatusAsync(List<Guid> userIds, ModerationAction action)
        {
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            if (users.Count == 0) return ApiResponse.Fail("No users found.");
            bool isBlocked = action == ModerationAction.Block;
            foreach (var user in users)
            {
                user.IsBlocked = isBlocked;
            }

            await _context.SaveChangesAsync();
            string result = isBlocked ? "Users successfully are blocked" : "Users successfully are unblocked";
            return ApiResponse.Ok(result);
        }

        public async Task<ApiResponse> DeleteUsersAsync(List<Guid> userIds)
        {
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            if (users.Count == 0)
            {
                return ApiResponse.Fail("No matching users found.");
            }

            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok($"Successfully deleted {users.Count} users.");
        }

        public async Task<ApiResponse> DeleteUnverifiedUsersAsync()
        {
            var unverifiedUsers = await _context.Users
                .Where(u => u.Status == UserStatus.Unverified)
                .ToListAsync();

            if (unverifiedUsers.Count == 0)
            {
                return ApiResponse.Fail("No unverified users found.");
            }

            _context.Users.RemoveRange(unverifiedUsers);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok($"Successfully deleted {unverifiedUsers.Count} unverified users.");
        }

        public async Task<ApiResponse> SendVerificationCodeAsync(string clientEmail)
        {
            var code = GenerateVerificationCode();
            await _emailService.SendVerificationEmailAsync(clientEmail, code);

            var redisKey = $"verification:{clientEmail}";
            var storedData = $"{code}:{DateTime.UtcNow.AddMinutes(5).Ticks}";
            await _redisDb.StringSetAsync(redisKey, storedData, TimeSpan.FromMinutes(5));

            return ApiResponse.Ok("Verification code sent successfully.");
        }

        private static string GenerateVerificationCode()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }

        public async Task<ApiResponse> VerifyCodeAsync(string clientEmail, string code)
        {
            var redisKey = $"verification:{clientEmail}";
            var value = await _redisDb.StringGetAsync(redisKey);

            if (value.IsNull)
            {
                return ApiResponse.Fail("Verification code is invalid or expired");
            }

            var parts = value.ToString().Split(':');
            if (parts.Length < 2 || parts[0] != code)
            {
                return ApiResponse.Fail("Verification code is invalid or expired");
            }

            if (!long.TryParse(parts[1], out var expiryTicks) || DateTime.UtcNow.Ticks > expiryTicks)
            {
                await _redisDb.KeyDeleteAsync(redisKey);
                return ApiResponse.Fail("Verification code is invalid or expired");
            }

            await _redisDb.KeyDeleteAsync(redisKey);

            var user = await _context.Users.FirstOrDefaultAsync(a => a.Email == clientEmail);
            if (user != null)
            {
                user.Status = UserStatus.Active;
                await _context.SaveChangesAsync();
            }
            return ApiResponse.Ok("User is successfully verified");
        }
    }
}