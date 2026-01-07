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

        // These checks are tenant-safe automatically because of global query filters
        var candidateExists = await _db.Candidates.AnyAsync(x => x.Id == request.CandidateId, ct);
        if (!candidateExists) return BadRequest("Candidate not found (or not in this tenant).");

        var jobExists = await _db.JobPostings.AnyAsync(x => x.Id == request.JobPostingId, ct);
        if (!jobExists) return BadRequest("Job not found (or not in this tenant).");

        // Prevent duplicate application (also enforced by unique index)
        var duplicate = await _db.JobApplications.AnyAsync(
            x => x.CandidateId == request.CandidateId && x.JobPostingId == request.JobPostingId,
            ct);

        if (duplicate) return Conflict("Candidate already applied to this job.");

        var app = new JobApplication
        {
            CandidateId = request.CandidateId,
            JobPostingId = request.JobPostingId,
            Status = "Submitted",
            Notes = request.Notes?.Trim()
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

        var app = await _db.JobApplications.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (app is null) return NotFound();

        app.Status = request.Status.Trim();
        app.Notes = request.Notes?.Trim();

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
}
