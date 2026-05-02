using System.Text.RegularExpressions;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using ERecruitment.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/public/{tenantSlug}")]
[AllowAnonymous]
public sealed class PublicCareerController : ControllerBase
{
    private const long MaxResumeBytes = 10 * 1024 * 1024;

    // Allowed (content-type, magic-byte signatures). The first matching prefix wins.
    private static readonly Dictionary<string, byte[][]> AllowedResumeSignatures = new()
    {
        ["application/pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }, // %PDF
        ["application/msword"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }, // legacy .doc OLE compound
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = new[]
            { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } // .docx (ZIP)
    };

    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled);

    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly TenantProvider _tenantProvider;
    private readonly IEmailNotificationService _emailNotifications;
    private readonly IAuditLogger _audit;

    public PublicCareerController(
        IApplicationDbContext db,
        ITenantContext tenant,
        TenantProvider tenantProvider,
        IEmailNotificationService emailNotifications,
        IAuditLogger audit)
    {
        _db = db;
        _tenant = tenant;
        _tenantProvider = tenantProvider;
        _emailNotifications = emailNotifications;
        _audit = audit;
    }

    // Resolves an active tenant by slug and binds it to the request scope.
    // Returns null and sets an action result when the tenant is missing/disabled.
    private async Task<Tenant?> ResolveActiveTenantAsync(string tenantSlug, CancellationToken ct)
    {
        var slug = (tenantSlug ?? "").Trim().ToLowerInvariant();
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug, ct);

        if (tenant is null || !tenant.IsActive) return null;

        _tenantProvider.SetTenant(tenant.Id);
        return tenant;
    }

    [HttpGet("jobs/get-all")]
    public async Task<IActionResult> GetJobs(string tenantSlug, CancellationToken ct)
    {
        var tenant = await ResolveActiveTenantAsync(tenantSlug, ct);
        if (tenant is null) return NotFound(new { error = "Tenant not found." });

        var jobs = await _db.JobPostings
            .AsNoTracking()
            .Where(j => j.Status == "Published")
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new
            {
                j.Id,
                j.Title,
                j.Department,
                j.Location,
                j.Description,
                j.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(jobs);
    }

    [HttpGet("jobs/{jobId:guid}")]
    public async Task<IActionResult> GetJob(string tenantSlug, Guid jobId, CancellationToken ct)
    {
        var tenant = await ResolveActiveTenantAsync(tenantSlug, ct);
        if (tenant is null) return NotFound(new { error = "Tenant not found." });

        var job = await _db.JobPostings
            .AsNoTracking()
            .Where(j => j.Id == jobId && j.Status == "Published")
            .Select(j => new
            {
                j.Id,
                j.Title,
                j.Department,
                j.Location,
                j.Description,
                j.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (job is null) return NotFound(new { error = "Job not found." });
        return Ok(job);
    }

    [HttpPost("jobs/{jobId:guid}/apply")]
    [RequestSizeLimit(MaxResumeBytes + 64 * 1024)] // small headroom for form fields
    public async Task<IActionResult> Apply(
        string tenantSlug,
        Guid jobId,
        [FromForm] PublicApplyRequest req,
        CancellationToken ct)
    {
        var tenant = await ResolveActiveTenantAsync(tenantSlug, ct);
        if (tenant is null) return NotFound(new { error = "Tenant not found." });

        var job = await _db.JobPostings
            .FirstOrDefaultAsync(x => x.Id == jobId && x.Status == "Published", ct);
        if (job is null) return NotFound(new { error = "Job not found." });

        // Field validation
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest(new { error = "FullName required." });
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new { error = "Email required." });
        if (!EmailRegex.IsMatch(req.Email.Trim())) return BadRequest(new { error = "Email is not a valid address." });
        if (string.IsNullOrWhiteSpace(req.Phone)) return BadRequest(new { error = "Phone required." });
        if (req.Resume is null || req.Resume.Length == 0) return BadRequest(new { error = "Resume file required." });

        // Resume validation
        if (req.Resume.Length > MaxResumeBytes)
            return BadRequest(new { error = $"Resume too large. Max {MaxResumeBytes / 1024 / 1024} MB." });

        var contentType = (req.Resume.ContentType ?? "").ToLowerInvariant();
        if (!AllowedResumeSignatures.TryGetValue(contentType, out var signatures))
            return BadRequest(new { error = "Unsupported file type. Allowed: PDF, DOC, DOCX." });

        // Magic-byte verification — don't trust client content-type alone
        var sniffBuffer = new byte[16];
        await using (var probe = req.Resume.OpenReadStream())
        {
            var read = await probe.ReadAsync(sniffBuffer.AsMemory(0, sniffBuffer.Length), ct);
            if (read < 4 || !signatures.Any(sig => sniffBuffer.Take(sig.Length).SequenceEqual(sig)))
                return BadRequest(new { error = "File contents do not match the declared type." });
        }

        var email = req.Email.Trim().ToLowerInvariant();

        // Find or create candidate (tenant filter is active)
        var candidate = await _db.Candidates.FirstOrDefaultAsync(x => x.Email == email, ct);
        if (candidate is null)
        {
            candidate = new Candidate
            {
                FullName = req.FullName.Trim(),
                Email = email,
                Phone = req.Phone.Trim(),
                AddressLine = req.AddressLine?.Trim() ?? "",
                PreviousCompanyName = req.PreviousCompanyName?.Trim(),
                NoOfYearExperience = req.NoOfYearExperience,
                InstituteName = req.InstituteName?.Trim() ?? "",
                Subject = req.Subject?.Trim() ?? "",
                ExpectedSalary = req.ExpectedSalary,
                SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? "BDT" : req.SalaryCurrency.Trim()
            };
            _db.Candidates.Add(candidate);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            candidate.FullName = req.FullName.Trim();
            candidate.Phone = req.Phone.Trim();
            candidate.AddressLine = req.AddressLine?.Trim() ?? candidate.AddressLine;
            candidate.PreviousCompanyName = req.PreviousCompanyName?.Trim();
            candidate.NoOfYearExperience = req.NoOfYearExperience;
            candidate.InstituteName = req.InstituteName?.Trim() ?? candidate.InstituteName;
            candidate.Subject = req.Subject?.Trim() ?? candidate.Subject;
            candidate.ExpectedSalary = req.ExpectedSalary;
            candidate.SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency)
                ? candidate.SalaryCurrency
                : req.SalaryCurrency.Trim();
        }

        // Block duplicate submission (matches the unique index on tenant+candidate+job)
        var alreadyApplied = await _db.JobApplications
            .AnyAsync(a => a.CandidateId == candidate.Id && a.JobPostingId == job.Id, ct);
        if (alreadyApplied)
            return Conflict(new { error = "You have already applied to this job." });

        // Save resume with a server-generated filename to prevent path traversal
        var ext = contentType switch
        {
            "application/pdf" => ".pdf",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            _ => ".bin"
        };
        var savedFileName = $"{Guid.NewGuid():N}{ext}";

        var folder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot", "uploads", tenant.Id.ToString(), "candidates", candidate.Id.ToString());
        Directory.CreateDirectory(folder);

        var savedPath = Path.Combine(folder, savedFileName);
        await using (var stream = System.IO.File.Create(savedPath))
        {
            await req.Resume.CopyToAsync(stream, ct);
        }

        var urlPath = $"/uploads/{tenant.Id}/candidates/{candidate.Id}/{savedFileName}";

        candidate.ResumeFileName = savedFileName;
        candidate.ResumeContentType = contentType;
        candidate.ResumeSize = req.Resume.Length;
        candidate.ResumeUrl = urlPath;

        var app = new JobApplication
        {
            CandidateId = candidate.Id,
            JobPostingId = job.Id,
            Status = "Submitted",
            Notes = req.Notes,
            ExpectedSalary = req.ExpectedSalary,
            SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? "BDT" : req.SalaryCurrency.Trim(),
            ResumeUrlSnapshot = urlPath,
            ResumeFileNameSnapshot = savedFileName,
            ResumeContentTypeSnapshot = contentType,
            ResumeSizeSnapshot = req.Resume.Length
        };
        _db.JobApplications.Add(app);
        await _db.SaveChangesAsync(ct);

        await _emailNotifications.SendApplicationReceivedAsync(app.Id, ct);
        await _audit.LogAsync(
            "Application.AppliedPublic",
            "JobApplication",
            app.Id,
            summary: $"Public apply: {candidate.Email}",
            data: new
            {
                ApplicationId = app.Id,
                CandidateEmail = candidate.Email,
                JobId = job.Id,
                JobTitle = job.Title
            },
            ct: ct);

        return Ok(new
        {
            message = "Application submitted",
            applicationId = app.Id,
            // Short human-readable reference so the candidate can quote it
            referenceCode = app.Id.ToString("N").Substring(0, 8).ToUpperInvariant()
        });
    }

    [HttpGet("theme")]
    public async Task<IActionResult> GetTheme(string tenantSlug, CancellationToken ct)
    {
        var tenant = await ResolveActiveTenantAsync(tenantSlug, ct);
        if (tenant is null) return NotFound(new { error = "Tenant not found." });

        var theme = await _db.TenantThemeSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenant.Id, ct);

        if (theme == null)
        {
            return Ok(new
            {
                CompanyName = tenant.Name,
                LogoUrl = (string?)null,
                FaviconUrl = (string?)null,
                PrimaryColor = "#1976d2",
                SecondaryColor = "#9c27b0",
                BackgroundColor = "#ffffff",
                FontFamily = "Inter",
                CustomCss = (string?)null
            });
        }

        return Ok(new
        {
            theme.CompanyName,
            theme.LogoUrl,
            theme.FaviconUrl,
            theme.PrimaryColor,
            theme.SecondaryColor,
            theme.BackgroundColor,
            theme.FontFamily,
            theme.CustomCss
        });
    }
}

public sealed class PublicApplyRequest
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? AddressLine { get; set; }

    public string? PreviousCompanyName { get; set; }
    public int? NoOfYearExperience { get; set; }

    public string? InstituteName { get; set; }
    public string? Subject { get; set; }

    public decimal? ExpectedSalary { get; set; }
    public string? SalaryCurrency { get; set; }

    public string? Notes { get; set; }

    public IFormFile Resume { get; set; } = default!;
}
