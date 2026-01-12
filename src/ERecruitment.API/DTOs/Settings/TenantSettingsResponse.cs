namespace ERecruitment.API.DTOs.Settings;

public sealed class TenantSettingsResponse
{
    public Guid TenantId { get; set; }
    public string CompanyName { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = default!;
    public bool CareerPageEnabled { get; set; }

    public int MaxResumeSizeMb { get; set; }
    public string AllowedResumeTypes { get; set; } = default!;
    public string TimeZone { get; set; } = default!;
}
