using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class AppUser : AuditableEntity
{
    public Guid? TenantId { get; set; } // ✅ nullable so SuperAdmin can be null

    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = "Admin";  // Admin/Recruiter/HiringManager/SuperAdmin
    public bool IsActive { get; set; } = true;
}
