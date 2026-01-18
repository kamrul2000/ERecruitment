namespace ERecruitment.API.DTOs.Auth;

public sealed class SuperAdminLoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}