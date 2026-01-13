using ERecruitment.API.DTOs.Jobs;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;

    public JobsController(IApplicationDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var jobs = await _db.JobPostings
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var job = await _db.JobPostings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return job is null ? NotFound() : Ok(job);
    }

    [Authorize(Roles = "Admin,Recruiter")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobPostingRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Department))
            return BadRequest("Department is required.");

        var job = new JobPosting
        {
            Title = request.Title.Trim(),
            Department = request.Department.Trim(),
            Location = request.Location?.Trim(),
            Description = request.Description?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Draft" : request.Status.Trim()
        };

        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Job.Created", "JobPosting", job.Id, $"Created job: {job.Title}", new { job.Id, job.Title }, ct);

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    [Authorize(Roles = "Admin,Recruiter")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobPostingRequest request, CancellationToken ct)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (job is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Department))
            return BadRequest("Department is required.");

        job.Title = request.Title.Trim();
        job.Department = request.Department.Trim();
        job.Location = request.Location?.Trim();
        job.Description = request.Description?.Trim();
        job.Status = string.IsNullOrWhiteSpace(request.Status) ? job.Status : request.Status.Trim();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
    
    [Authorize(Roles = "Admin,Recruiter")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (job is null) return NotFound();

        _db.JobPostings.Remove(job);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
