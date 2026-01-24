using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class TenantThemeSettings : BaseEntity
{
    public string CompanyName { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }

    // Theme
    public string PrimaryColor { get; set; } = "#1976d2";
    public string SecondaryColor { get; set; } = "#9c27b0";
    public string BackgroundColor { get; set; } = "#ffffff";

    // Optional: fonts + layout
    public string FontFamily { get; set; } = "Inter";
    public string Template { get; set; } = "Default"; // Default / Modern / Minimal

    // Optional: custom CSS (keep limited/safe)
    public string? CustomCss { get; set; }
}
