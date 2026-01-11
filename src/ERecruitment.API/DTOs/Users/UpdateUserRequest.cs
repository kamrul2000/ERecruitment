namespace ERecruitment.API.DTOs.Users;

public sealed class UpdateUserRequest
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = "Recruiter";
    public bool IsActive { get; set; } = true;
}
