using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public class AppUser : BaseEntity
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;

    // Hashed password (never store plain password)
    public string PasswordHash { get; set; } = default!;

    // "Admin" | "Recruiter" | "HiringManager"
    public string Role { get; set; } = "Recruiter";

    public bool IsActive { get; set; } = true;
}
