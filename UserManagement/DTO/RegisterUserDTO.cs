using UserManagement.Domain;

namespace UserManagement.DTO
{
    public record RegisterUserDTO(string email, string plainPassword, string name);
}