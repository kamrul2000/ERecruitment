using ERecruitment.API.DTOs.Applications;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailNotificationService _emailNotifications;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _currentUser;

    public ApplicationsController(
        IApplicationDbContext db,
        IEmailNotificationService emailNotifications,
        IAuditLogger audit,
        ICurrentUser currentUser)
    {
        _db = db;
        _emailNotifications = emailNotifications;
        _audit = audit;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var apps = await _db.JobApplications
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(apps);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var app = await _db.JobApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return app is null ? NotFound() : Ok(app);
    }

    // Filter by job
    [HttpGet("by-job/{jobId:guid}")]
    public async Task<IActionResult> GetByJob(Guid jobId, CancellationToken ct)
    {
        var items = await _db.JobApplications
            .AsNoTracking()
            .Where(x => x.JobPostingId == jobId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(items);
    }

    // Filter by candidate
    [HttpGet("by-candidate/{candidateId:guid}")]
    public async Task<IActionResult> GetByCandidate(Guid candidateId, CancellationToken ct)
    {
        var items = await _db.JobApplications
            .AsNoTracking()
            .Where(x => x.CandidateId == candidateId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobApplicationRequest request, CancellationToken ct)
    {
        if (request.CandidateId == Guid.Empty) return BadRequest("CandidateId is required.");
        if (request.JobPostingId == Guid.Empty) return BadRequest("JobPostingId is required.");

        var candidate = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == request.CandidateId, ct);
        if (candidate is null) return BadRequest("Candidate not found (or not in this tenant).");

        var job = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == request.JobPostingId, ct);
        if (job is null) return BadRequest("Job not found (or not in this tenant).");

        // Prevent duplicate application (tenant-safe due to query filters)
        var duplicate = await _db.JobApplications.AnyAsync(
            x => x.CandidateId == request.CandidateId && x.JobPostingId == request.JobPostingId,
            ct);

        if (duplicate) return Conflict("Candidate already applied to this job.");

        // Candidate must have CV uploaded (recommended rule)
        if (string.IsNullOrWhiteSpace(candidate.ResumeUrl))
            return BadRequest("Candidate has no CV. Upload CV first.");

        var app = new JobApplication
        {
            CandidateId = candidate.Id,
            JobPostingId = job.Id,
            Status = "Submitted",

            ExpectedSalary = request.ExpectedSalary ?? candidate.ExpectedSalary,
            SalaryCurrency = string.IsNullOrWhiteSpace(request.SalaryCurrency) ? "BDT" : request.SalaryCurrency.Trim(),
            Notes = request.Notes?.Trim(),

            // Snapshot CV at apply time
            ResumeUrlSnapshot = candidate.ResumeUrl,
            ResumeFileNameSnapshot = candidate.ResumeFileName,
            ResumeContentTypeSnapshot = candidate.ResumeContentType,
            ResumeSizeSnapshot = candidate.ResumeSize
        };

        _db.JobApplications.Add(app);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = app.Id }, app);
    }

    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateJobApplicationStatusRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest("Status is required.");

        var allowed = new[] { "Submitted", "Reviewed", "Shortlisted", "Rejected", "Hired" };
        var newStatus = request.Status.Trim();

        if (!allowed.Contains(newStatus))
            return BadRequest("Invalid status. Allowed: Submitted, Reviewed, Shortlisted, Rejected, Hired.");

        var app = await _db.JobApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (app is null) return NotFound();

        var oldStatus = app.Status;

        if (string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            return BadRequest("New status is same as current status.");

        // Update application
        app.Status = newStatus;
        app.Notes = request.Notes?.Trim();

        // Insert history record
        var actor = _currentUser.Email ?? _currentUser.UserId?.ToString();
        var history = new JobApplicationStatusHistory
        {
            JobApplicationId = app.Id,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Comment = request.Notes?.Trim(),
            ChangedBy = actor
        };

        _db.JobApplicationStatusHistories.Add(history);

        await _db.SaveChangesAsync(ct);
        // Notify the candidate that their application status changed. (Previously this
        // wrongly sent the "Application Received" template on every status change.)
        await _emailNotifications.SendStatusChangedAsync(app.Id, newStatus, request.Notes?.Trim(), ct);
        await _audit.LogAsync(
    action: "Application.StatusChanged",
    entityType: "JobApplication",
    entityId: app.Id,
    summary: $"Status changed to {request.Status}",
    data: new { app.Id, request.Status, request.Notes },
    ct: ct);


        return NoContent();
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var app = await _db.JobApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (app is null) return NotFound();

        _db.JobApplications.Remove(app);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken ct)
    {
        // Ensure the application exists in this tenant
        var exists = await _db.JobApplications.AnyAsync(x => x.Id == id, ct);
        if (!exists) return NotFound();

        var items = await _db.JobApplicationStatusHistories
            .AsNoTracking()
            .Where(x => x.JobApplicationId == id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("/api/jobs/{jobId:guid}/applications/search")]
    public async Task<IActionResult> SearchApplications(
    Guid jobId,
    [FromBody] ApplicationFilterQuery q,
    CancellationToken ct)


    {
        // Validate paging
        if (q.Page < 1) q.Page = 1;
        if (q.PageSize < 1) q.PageSize = 20;
        if (q.PageSize > 100) q.PageSize = 100;

        // Ensure job exists in this tenant
        var jobExists = await _db.JobPostings.AnyAsync(x => x.Id == jobId, ct);
        if (!jobExists) return NotFound("Job not found (or not in this tenant).");

        var query =
            from app in _db.JobApplications.AsNoTracking()
            join cand in _db.Candidates.AsNoTracking()
                on app.CandidateId equals cand.Id
            where app.JobPostingId == jobId
            select new
            {
                ApplicationId = app.Id,
                app.JobPostingId,
                app.CandidateId,
                app.Status,
                app.CreatedAt,
                app.UpdatedAt,

                // Salary from application (if you added it in Phase 6A)
                app.ExpectedSalary,
                app.SalaryCurrency,

                // CV snapshot
                cand.ResumeUrl,

                // Candidate info
                CandidateName = cand.FullName,
                CandidateEmail = cand.Email,
                CandidatePhone = cand.Phone,
                ExperienceYears = cand.NoOfYearExperience
            };

        // Status filter (supports comma separated list)
        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            var statuses = q.Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.Trim())
                .ToArray();

            if (statuses.Length > 0)
                query = query.Where(x => statuses.Contains(x.Status));
        }

        // Salary filters
        if (q.MinSalary.HasValue) query = query.Where(x => x.ExpectedSalary >= q.MinSalary.Value);
        if (q.MaxSalary.HasValue) query = query.Where(x => x.ExpectedSalary <= q.MaxSalary.Value);

        // Experience filters
        if (q.MinExperienceYears.HasValue) query = query.Where(x => x.ExperienceYears >= q.MinExperienceYears.Value);
        if (q.MaxExperienceYears.HasValue) query = query.Where(x => x.ExperienceYears <= q.MaxExperienceYears.Value);

        // Keyword filter (name/email/phone)
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var k = q.Keyword.Trim();

            query = query.Where(x =>
                EF.Functions.Like(x.CandidateName, $"%{k}%") ||
                EF.Functions.Like(x.CandidateEmail, $"%{k}%") ||
                EF.Functions.Like(x.CandidatePhone, $"%{k}%"));
        }

        // Sorting
        var desc = string.Equals(q.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        query = q.SortBy.ToLowerInvariant() switch
        {
            "salary" => desc ? query.OrderByDescending(x => x.ExpectedSalary) : query.OrderBy(x => x.ExpectedSalary),
            "experience" => desc ? query.OrderByDescending(x => x.ExperienceYears) : query.OrderBy(x => x.ExperienceYears),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        return Ok(new
        {
            total,
            page = q.Page,
            pageSize = q.PageSize,
            items
        });
    }


    [HttpPost("search")]
    public async Task<IActionResult> Search(
    [FromBody] ApplicationFilterQuery q,
    CancellationToken ct)
    {
        if (q.Page < 1) q.Page = 1;
        if (q.PageSize < 1) q.PageSize = 20;
        if (q.PageSize > 100) q.PageSize = 100;

        var query =
            from app in _db.JobApplications.AsNoTracking()
            join cand in _db.Candidates.AsNoTracking() on app.CandidateId equals cand.Id
            join job in _db.JobPostings.AsNoTracking() on app.JobPostingId equals job.Id
            select new
            {
                ApplicationId = app.Id,
                app.JobPostingId,
                JobTitle = job.Title,
                Department = job.Department,

                app.CandidateId,
                CandidateName = cand.FullName,
                CandidateEmail = cand.Email,
                CandidatePhone = cand.Phone,
                ExperienceYears = cand.NoOfYearExperience,

                app.Status,
                app.CreatedAt,
                app.ExpectedSalary,
                app.SalaryCurrency,
                cand.ResumeUrl,
                app.ResumeUrlSnapshot
            };

        // Status
        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            var statuses = q.Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.Trim())
                .ToArray();

            if (statuses.Length > 0)
                query = query.Where(x => statuses.Contains(x.Status));
        }

        // Salary
        if (q.MinSalary.HasValue) query = query.Where(x => x.ExpectedSalary >= q.MinSalary.Value);
        if (q.MaxSalary.HasValue) query = query.Where(x => x.ExpectedSalary <= q.MaxSalary.Value);

        // Experience
        if (q.MinExperienceYears.HasValue) query = query.Where(x => x.ExperienceYears >= q.MinExperienceYears.Value);
        if (q.MaxExperienceYears.HasValue) query = query.Where(x => x.ExperienceYears <= q.MaxExperienceYears.Value);

        // Keyword
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var k = q.Keyword.Trim();

            query = query.Where(x =>
                EF.Functions.Like(x.CandidateName, $"%{k}%") ||
                EF.Functions.Like(x.CandidateEmail, $"%{k}%") ||
                EF.Functions.Like(x.CandidatePhone, $"%{k}%") ||
                EF.Functions.Like(x.JobTitle, $"%{k}%"));
        }

        // Sorting
        var desc = string.Equals(q.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        query = q.SortBy.ToLowerInvariant() switch
        {
            "salary" => desc ? query.OrderByDescending(x => x.ExpectedSalary) : query.OrderBy(x => x.ExpectedSalary),
            "experience" => desc ? query.OrderByDescending(x => x.ExperienceYears) : query.OrderBy(x => x.ExperienceYears),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        return Ok(new { total, page = q.Page, pageSize = q.PageSize, items });
    }


}
