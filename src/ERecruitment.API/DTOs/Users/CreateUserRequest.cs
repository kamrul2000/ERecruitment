namespace ERecruitment.API.DTOs.Users;

public sealed class CreateUserRequest
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Role { get; set; } = "Recruiter"; // Admin/Recruiter/HiringManager
    public bool IsActive { get; set; } = true;
}
