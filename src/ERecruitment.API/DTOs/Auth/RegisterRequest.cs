namespace ERecruitment.API.DTOs.Auth;

public sealed class RegisterRequest
{
    public string TenantSlug { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Role { get; set; } = "Recruiter";
}
