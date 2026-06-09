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

        public UserService(AppDbContext context, PasswordHasherService passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApiResponse<User>> RegisterAsync(RegisterUserDTO dto)
        {
            bool userExists = await _context.Users.AnyAsync(u => u.Email == dto.email);
            if (userExists)
            {
                return ApiResponse<User>.Fail("A user with this email already exists.");
            }

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

            return ApiResponse<User>.Ok(user, "The user has been successfully registered.");
        }
    }
}