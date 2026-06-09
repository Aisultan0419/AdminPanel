namespace UserManagement.Domain
{
    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserStatus Status { get; set; } = UserStatus.Unverified;
        public DateTime? LastActivityTime { get; set; }
    }
}
