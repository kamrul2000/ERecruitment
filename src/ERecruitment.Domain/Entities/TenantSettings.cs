using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class TenantSettings : BaseEntity
{
    public string CompanyName { get; set; } = "My Company";
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#3f51b5";

    // Public career page / candidate portal toggles (Phase later)
    public bool CareerPageEnabled { get; set; } = true;

    // File upload limits
    public int MaxResumeSizeMb { get; set; } = 10; // default 10MB
    public string AllowedResumeTypes { get; set; } = "pdf,doc,docx";

    // Optional
    public string TimeZone { get; set; } = "Asia/Dhaka";
}
