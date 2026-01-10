namespace ERecruitment.API.DTOs.Auth;

public sealed class LoginRequest
{
    public string TenantSlug { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}
