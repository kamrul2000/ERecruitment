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
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly TenantProvider _tenantProvider;
    private readonly IEmailNotificationService _emailNotifications;
    private readonly IAuditLogger _audit;



    public PublicCareerController(IApplicationDbContext db, ITenantContext tenant, TenantProvider tenantProvider, IEmailNotificationService emailNotifications, IAuditLogger audit)
    {
        _db = db;
        _tenant = tenant;
        _tenantProvider = tenantProvider;
        _emailNotifications = emailNotifications;
        _audit = audit;
    }

    // GET: api/public/{tenantSlug}/jobs
    [HttpGet("jobs/get-all")]
    public async Task<IActionResult> GetJobs(string tenantSlug, Guid jobId, CancellationToken ct)
    {
        //var tenant = await _db.Tenants.AsNoTracking()
        //    .FirstOrDefaultAsync(x => x.Slug == tenantSlug.ToLower(), ct);

        //if (tenant is null || !tenant.IsActive) return NotFound("Tenant not found.");

        //_tenant.SetTenant(tenant.Id);

        //// Only show Published jobs publicly (adjust if your enum differs)
        //var jobs = await _db.JobPostings.AsNoTracking()
        //    .Where(j => j.Status == "Published")
        //    .OrderByDescending(x => x.CreatedAt)
        //    .Select(j => new
        //    {
        //        j.Id,
        //        j.Title,
        //        j.Department,
        //        j.Location,
        //        j.Description,
        //        j.Status,
        //        j.CreatedAt
        //    })
        //    .ToListAsync(ct);

        //return Ok(jobs);

        // Resolve tenant slug (case-insensitive)
        var tenantId = await _db.Tenants
            .Where(x => x.Slug.ToLower() == tenantSlug.ToLower())
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (tenantId == Guid.Empty)
            return NotFound("Tenant not found.");

        // Set tenant before querying EF
        _tenantProvider.SetTenant(tenantId);

        // Fetch only published jobs
        var jobs = await _db.JobPostings
            .AsNoTracking()
            .Where(j => j.Status == "Published")
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

        if (!jobs.Any())
            return NotFound("Job not found.");

        return Ok(jobs);
    }

    // GET: api/public/{tenantSlug}/jobs/{jobId}
    [HttpGet("jobs/{jobId:guid}")]
    public async Task<IActionResult> GetJob(string tenantSlug, Guid jobId, CancellationToken ct)
    {
        //var tenant = await _db.Tenants.AsNoTracking()
        //    .FirstOrDefaultAsync(x => x.Slug == tenantSlug.ToLower(), ct);

        //if (tenant is null || !tenant.IsActive) return NotFound("Tenant not found.");

        //_tenant.SetTenant(tenant.Id);

        //var job = await _db.JobPostings.AsNoTracking()
        //    .Where(j => j.Id == jobId && j.Status == "Published")
        //    .Select(j => new
        //    {
        //        j.Id,
        //        j.Title,
        //        j.Department,
        //        j.Location,
        //        j.Description,
        //        j.Status,
        //        j.CreatedAt
        //    })
        //    .FirstOrDefaultAsync(ct);

        //if (job is null) return NotFound("Job not found.");
        //return Ok(job);

        // 🔹 Resolve tenant slug (case-insensitive)
        var tenantId = await _db.Tenants
            .Where(x => x.Slug.ToLower() == tenantSlug.ToLower())
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (tenantId == Guid.Empty)
            return NotFound("Tenant not found.");

        // 🔹 Set tenant BEFORE querying JobPostings
        _tenantProvider.SetTenant(tenantId);

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
                j.Status,
                j.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (job is null) return NotFound("Job not found.");
        return Ok(job);
    }

    // POST: api/public/{tenantSlug}/jobs/{jobId}/apply
    // multipart/form-data (includes file)
    [HttpPost("jobs/{jobId:guid}/apply")]
    [RequestSizeLimit(30_000_000)] // keep high; we enforce tenant settings below
    public async Task<IActionResult> Apply(string tenantSlug, Guid jobId, [FromForm] PublicApplyRequest req, CancellationToken ct)
    {
        //    var tenant = await _db.Tenants.AsNoTracking()
        //        .FirstOrDefaultAsync(x => x.Slug == tenantSlug.ToLower(), ct);

        //    if (tenant is null || !tenant.IsActive) return NotFound("Tenant not found.");

        //    _tenant.SetTenant(tenant.Id);

        //    var job = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == jobId && x.Status == "Published", ct);
        //    if (job is null) return NotFound("Job not found.");

        //    if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest("FullName required.");
        //    if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest("Email required.");
        //    if (string.IsNullOrWhiteSpace(req.Phone)) return BadRequest("Phone required.");

        //    // Validate resume based on tenant settings (Phase 8B)
        //    var settings = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        //    var maxMb = settings?.MaxResumeSizeMb ?? 10;
        //    var allowedExt = (settings?.AllowedResumeTypes ?? "pdf,doc,docx")
        //        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        //        .Select(x => x.ToLower()).ToHashSet();

        //    if (req.Resume is null || req.Resume.Length == 0) return BadRequest("Resume file required.");

        //    if (req.Resume.Length > maxMb * 1024L * 1024L)
        //        return BadRequest($"Resume too large. Max allowed: {maxMb} MB.");

        //    var ext = Path.GetExtension(req.Resume.FileName).TrimStart('.').ToLowerInvariant();
        //    if (!allowedExt.Contains(ext))
        //        return BadRequest($"Invalid file type. Allowed: {string.Join(",", allowedExt)}");

        //    var email = req.Email.Trim().ToLowerInvariant();

        //    // Find or create candidate by email (tenant isolated by filter)
        //    var candidate = await _db.Candidates.FirstOrDefaultAsync(x => x.Email == email, ct);
        //    if (candidate is null)
        //    {
        //        candidate = new Candidate
        //        {
        //            FullName = req.FullName.Trim(),
        //            Email = email,
        //            Phone = req.Phone.Trim(),
        //            AddressLine = req.AddressLine?.Trim() ?? "",
        //            PreviousCompanyName = req.PreviousCompanyName?.Trim(),
        //            NoOfYearExperience = req.NoOfYearExperience,
        //            InstituteName = req.InstituteName?.Trim() ?? "",
        //            Subject = req.Subject?.Trim() ?? "",
        //            ExpectedSalary = req.ExpectedSalary,
        //            SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? "BDT" : req.SalaryCurrency.Trim()
        //        };

        //        _db.Candidates.Add(candidate);
        //        await _db.SaveChangesAsync(ct);
        //    }
        //    else
        //    {
        //        // Update minimal fields (optional)
        //        candidate.FullName = req.FullName.Trim();
        //        candidate.Phone = req.Phone.Trim();
        //        candidate.AddressLine = req.AddressLine?.Trim() ?? candidate.AddressLine;
        //        candidate.PreviousCompanyName = req.PreviousCompanyName?.Trim();
        //        candidate.NoOfYearExperience = req.NoOfYearExperience;
        //        candidate.InstituteName = req.InstituteName?.Trim() ?? candidate.InstituteName;
        //        candidate.Subject = req.Subject?.Trim() ?? candidate.Subject;
        //        candidate.ExpectedSalary = req.ExpectedSalary;
        //        candidate.SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? candidate.SalaryCurrency : req.SalaryCurrency.Trim();
        //    }

        //    // Save resume to tenant folder
        //    var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        //    var folder = Path.Combine(uploadsRoot, tenant.Id.ToString(), "candidates", candidate.Id.ToString());
        //    Directory.CreateDirectory(folder);

        //    var safeFileName = Path.GetFileName(req.Resume.FileName);
        //    var savedPath = Path.Combine(folder, safeFileName);

        //    await using (var stream = System.IO.File.Create(savedPath))
        //    {
        //        await req.Resume.CopyToAsync(stream, ct);
        //    }

        //    var urlPath = $"/uploads/{tenant.Id}/candidates/{candidate.Id}/{Uri.EscapeDataString(safeFileName)}";

        //    candidate.ResumeFileName = safeFileName;
        //    candidate.ResumeContentType = req.Resume.ContentType;
        //    candidate.ResumeSize = req.Resume.Length;
        //    candidate.ResumeUrl = urlPath;

        //    // Create application (your existing entity)
        //    var app = new JobApplication
        //    {
        //        CandidateId = candidate.Id,
        //        JobPostingId = job.Id,
        //        Status = "Submitted",
        //        Notes = req.Notes,
        //        ExpectedSalary = req.ExpectedSalary,
        //        SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? "BDT" : req.SalaryCurrency.Trim(),

        //        // If you added snapshot columns, set them:
        //        // ResumeUrlSnapshot = urlPath
        //    };

        //    _db.JobApplications.Add(app);
        //    await _db.SaveChangesAsync(ct);

        //    return Ok(new
        //    {
        //        message = "Application submitted",
        //        applicationId = app.Id
        //    });
        //}

        // 🔹 Resolve tenant (case-insensitive)
        var tenantId = await _db.Tenants
            .Where(x => x.Slug.ToLower() == tenantSlug.ToLower())
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (tenantId == Guid.Empty)
            return NotFound("Tenant not found.");

        // 🔹 Set tenant BEFORE any tenant-scoped query
        _tenantProvider.SetTenant(tenantId);

        // 🔹 Fetch the job AFTER setting tenant
        var job = await _db.JobPostings
            .FirstOrDefaultAsync(x => x.Id == jobId && x.Status == "Published", ct);

        if (job is null)
            return NotFound("Job not found.");

        // Validate request
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest("FullName required.");
        if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest("Email required.");
        if (string.IsNullOrWhiteSpace(req.Phone)) return BadRequest("Phone required.");

        // Candidate lookup (tenant-scoped)
        var email = req.Email.Trim().ToLowerInvariant();
        var candidate = await _db.Candidates
            .FirstOrDefaultAsync(x => x.Email == email, ct);

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
            // Update minimal fields
            candidate.FullName = req.FullName.Trim();
            candidate.Phone = req.Phone.Trim();
            candidate.AddressLine = req.AddressLine?.Trim() ?? candidate.AddressLine;
            candidate.PreviousCompanyName = req.PreviousCompanyName?.Trim();
            candidate.NoOfYearExperience = req.NoOfYearExperience;
            candidate.InstituteName = req.InstituteName?.Trim() ?? candidate.InstituteName;
            candidate.Subject = req.Subject?.Trim() ?? candidate.Subject;
            candidate.ExpectedSalary = req.ExpectedSalary;
            candidate.SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? candidate.SalaryCurrency : req.SalaryCurrency.Trim();
        }

        // Save resume
        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        var folder = Path.Combine(uploadsRoot, tenantId.ToString(), "candidates", candidate.Id.ToString());
        Directory.CreateDirectory(folder);

        var safeFileName = Path.GetFileName(req.Resume.FileName);
        var savedPath = Path.Combine(folder, safeFileName);

        await using (var stream = System.IO.File.Create(savedPath))
        {
            await req.Resume.CopyToAsync(stream, ct);
        }

        var urlPath = $"/uploads/{tenantId}/candidates/{candidate.Id}/{Uri.EscapeDataString(safeFileName)}";

        candidate.ResumeFileName = safeFileName;
        candidate.ResumeContentType = req.Resume.ContentType;
        candidate.ResumeSize = req.Resume.Length;
        candidate.ResumeUrl = urlPath;

        // Create application
        var app = new JobApplication
        {
            CandidateId = candidate.Id,
            JobPostingId = job.Id,
            Status = "Submitted",
            Notes = req.Notes,
            ExpectedSalary = req.ExpectedSalary,
            SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? "BDT" : req.SalaryCurrency.Trim()
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
            applicationId = app.Id
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
