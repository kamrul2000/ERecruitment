using ERecruitment.API.DTOs.Applications;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public ApplicationsController(IApplicationDbContext db)
    {
        _db = db;
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
        var history = new JobApplicationStatusHistory
        {
            JobApplicationId = app.Id,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Comment = request.Notes?.Trim(),
            ChangedBy = null // later set from JWT user
        };

        _db.JobApplicationStatusHistories.Add(history);

        await _db.SaveChangesAsync(ct);
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

}
