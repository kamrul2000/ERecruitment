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

    // GET: api/jobs?page=&pageSize=&search=
    // Paginated + server-side search so the list can never load unbounded.
    // Returns { total, page, pageSize, items }.
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.JobPostings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var k = search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.Title, $"%{k}%") ||
                EF.Functions.Like(x.Department, $"%{k}%") ||
                EF.Functions.Like(x.Location, $"%{k}%") ||
                EF.Functions.Like(x.Status, $"%{k}%"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    // GET: api/jobs/stats — status breakdown for the dashboard without pulling
    // every job row just to count them.
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var byStatus = await _db.JobPostings
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int CountFor(string s) => byStatus
            .Where(x => string.Equals(x.Status, s, StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Count);

        return Ok(new
        {
            total = byStatus.Sum(x => x.Count),
            published = CountFor("Published"),
            draft = CountFor("Draft"),
            closed = CountFor("Closed")
        });
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
