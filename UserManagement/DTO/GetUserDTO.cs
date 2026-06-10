namespace UserManagement.DTO
{
    public record GetUserDTO(Guid Id, string Name, string Email, string Status, DateTime? LastActivityTime, bool IsBlocked);
}
