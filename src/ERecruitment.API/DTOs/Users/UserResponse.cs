namespace ERecruitment.API.DTOs.Users;

public sealed class UserResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
