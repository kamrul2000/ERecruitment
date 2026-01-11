namespace ERecruitment.API.DTOs.Users;

public sealed class ResetPasswordRequest
{
    public string NewPassword { get; set; } = default!;
}
