namespace ERecruitment.API.DTOs.Settings;

public sealed class UpdateTenantSettingsRequest
{
    public string CompanyName { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#3f51b5";
    public bool CareerPageEnabled { get; set; } = true;

    public int MaxResumeSizeMb { get; set; } = 10;
    public string AllowedResumeTypes { get; set; } = "pdf,doc,docx";
    public string TimeZone { get; set; } = "Asia/Dhaka";
}
