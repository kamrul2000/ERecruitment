using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using ERecruitment.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // tenant admin only
public sealed class TenantSettingsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _current;

    public TenantSettingsController(IApplicationDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    // GET: api/TenantSettings/theme
    [HttpGet("theme")]
    public async Task<IActionResult> GetTheme(CancellationToken ct)
    {
        var tenantId = _current.TenantId ?? throw new InvalidOperationException("TenantId is missing.");


        var theme = await _db.TenantThemeSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (theme is null)
        {
            // create default row for this tenant
            theme = new TenantThemeSettings
            {
                TenantId = tenantId,
                CompanyName = "Company",
                PrimaryColor = "#1976d2",
                SecondaryColor = "#9c27b0",
                BackgroundColor = "#ffffff",
                FontFamily = "Inter",
                Template = "Default"
            };

            _db.TenantThemeSettings.Add(theme);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(theme);
    }

    // PUT: api/TenantSettings/theme
    [HttpPut("theme")]
    public async Task<IActionResult> UpdateTheme([FromBody] UpdateTenantThemeRequest req, CancellationToken ct)
    {
        var tenantId = _current.TenantId ?? throw new InvalidOperationException("TenantId is missing.");

        var theme = await _db.TenantThemeSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (theme is null)
        {
            theme = new TenantThemeSettings { TenantId = tenantId };
            _db.TenantThemeSettings.Add(theme);
        }

        theme.CompanyName = req.CompanyName.Trim();
        theme.Template = req.Template.Trim();
        theme.FontFamily = req.FontFamily.Trim();
        theme.PrimaryColor = req.PrimaryColor.Trim();
        theme.SecondaryColor = req.SecondaryColor.Trim();
        theme.BackgroundColor = req.BackgroundColor.Trim();

        // IMPORTANT: do NOT force logoUrl to null if you upload separately
        if (!string.IsNullOrWhiteSpace(req.LogoUrl))
            theme.LogoUrl = req.LogoUrl.Trim();

        if (!string.IsNullOrWhiteSpace(req.FaviconUrl))
            theme.FaviconUrl = req.FaviconUrl.Trim();

        theme.CustomCss = string.IsNullOrWhiteSpace(req.CustomCss) ? null : req.CustomCss;

        await _db.SaveChangesAsync(ct);
        return Ok(theme);
    }

    // POST: api/TenantSettings/theme/logo
    [HttpPost("theme/logo")]
    [RequestSizeLimit(5_000_000)] // 5MB
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("File is required.");

        var allowed = new[] { "image/png", "image/jpeg", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest("Only PNG, JPG, WEBP allowed.");

        var tenantId = _current.TenantId ?? throw new InvalidOperationException("TenantId is missing.");

        // ✅ store in wwwroot/uploads/{tenantId}/branding/
        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        var folder = Path.Combine(uploadsRoot, tenantId.ToString(), "branding");
        Directory.CreateDirectory(folder);

        // ✅ stable filename so overwrite works (no duplicates)
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
        var savedName = "logo" + ext.ToLowerInvariant();
        var savedPath = Path.Combine(folder, savedName);

        await using (var stream = System.IO.File.Create(savedPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var urlPath = $"/uploads/{tenantId}/branding/{Uri.EscapeDataString(savedName)}";

        var theme = await _db.TenantThemeSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (theme is null)
        {
            theme = new TenantThemeSettings { TenantId = tenantId };
            _db.TenantThemeSettings.Add(theme);
        }

        theme.LogoUrl = urlPath;

        await _db.SaveChangesAsync(ct);

        return Ok(new { logoUrl = urlPath });
    }
}

public sealed class UpdateTenantThemeRequest
{
    public string CompanyName { get; set; } = default!;
    public string Template { get; set; } = "Default";
    public string FontFamily { get; set; } = "Inter";
    public string PrimaryColor { get; set; } = "#1976d2";
    public string SecondaryColor { get; set; } = "#9c27b0";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CustomCss { get; set; }
}
