namespace ERecruitment.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? TenantId { get; }  // from JWT claim
    bool IsAuthenticated { get; }
}
