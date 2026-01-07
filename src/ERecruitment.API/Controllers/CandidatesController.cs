using ERecruitment.API.Contracts.Candidates;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CandidatesController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public CandidatesController(IApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/candidates
    [HttpGet]
    public async Task<ActionResult<List<CandidateResponse>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Candidates
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CandidateResponse
            {
                Id = x.Id,
                TenantId = x.TenantId,
                FullName = x.FullName,
                Email = x.Email,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // GET: api/candidates/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CandidateResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _db.Candidates
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CandidateResponse
            {
                Id = x.Id,
                TenantId = x.TenantId,
                FullName = x.FullName,
                Email = x.Email,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null) return NotFound();
        return Ok(item);
    }

    // POST: api/candidates
    [HttpPost]
    public async Task<ActionResult<CandidateResponse>> Create([FromBody] CandidateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest("FullName is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");

        var entity = new Candidate
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim()
            // TenantId/CreatedAt/UpdatedAt set automatically by interceptor
        };
        _db.Candidates.Add(entity);
        await _db.SaveChangesAsync(ct);

        var response = new CandidateResponse
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            FullName = entity.FullName,
            Email = entity.Email,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    // PUT: api/candidates/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CandidateRequest request, CancellationToken ct)
    {
        var entity = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        entity.FullName = request.FullName.Trim();
        entity.Email = request.Email.Trim();
        // UpdatedAt handled automatically by interceptor

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/candidates/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        _db.Candidates.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
