using ERecruitment.API.Contracts.Candidates;
using ERecruitment.API.DTOs.Candidates;
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
                Phone = x.Phone,
                ExpectedSalary = x.ExpectedSalary,
                ResumeFileName = x.ResumeFileName,
                ResumeContentType = x.ResumeContentType,
                ResumeSize = x.ResumeSize,
                ResumeUrl = x.ResumeUrl,
                NoOfYearExperience = x.NoOfYearExperience,
                InstituteName = x.InstituteName,
                Subject = x.Subject,
                AddressLine=x.AddressLine,

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
    //[HttpPost]
    //public async Task<ActionResult<CandidateResponse>> Create([FromBody] CandidateRequest request, CancellationToken ct)
    //{
    //    if (string.IsNullOrWhiteSpace(request.FullName))
    //        return BadRequest("FullName is required.");

    //    if (string.IsNullOrWhiteSpace(request.Email))
    //        return BadRequest("Email is required.");

    //    var entity = new Candidate
    //    {
    //        FullName = request.FullName.Trim(),
    //        Email = request.Email.Trim()
    //        // TenantId/CreatedAt/UpdatedAt set automatically by interceptor
    //    };
    //    _db.Candidates.Add(entity);
    //    await _db.SaveChangesAsync(ct);

    //    var response = new CandidateResponse
    //    {
    //        Id = entity.Id,
    //        TenantId = entity.TenantId,
    //        FullName = entity.FullName,
    //        Email = entity.Email,
    //        CreatedAt = entity.CreatedAt,
    //        UpdatedAt = entity.UpdatedAt
    //    };

    //    return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    //}

    //// PUT: api/candidates/{id}
    //[HttpPut("{id:guid}")]
    //public async Task<IActionResult> Update(Guid id, [FromBody] CandidateRequest request, CancellationToken ct)
    //{
    //    var entity = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == id, ct);
    //    if (entity is null) return NotFound();

    //    entity.FullName = request.FullName.Trim();
    //    entity.Email = request.Email.Trim();
    //    // UpdatedAt handled automatically by interceptor

    //    await _db.SaveChangesAsync(ct);
    //    return NoContent();
    //}

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
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertCandidateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest("FullName required");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email required");
        if (string.IsNullOrWhiteSpace(request.Phone)) return BadRequest("Phone required");
        if (string.IsNullOrWhiteSpace(request.AddressLine)) return BadRequest("AddressLine required");
        if (string.IsNullOrWhiteSpace(request.InstituteName)) return BadRequest("InstituteName required");
        if (string.IsNullOrWhiteSpace(request.Subject)) return BadRequest("Subject required");

        var candidate = new Candidate
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            AddressLine = request.AddressLine.Trim(),

            PreviousCompanyName = request.PreviousCompanyName?.Trim(),
            NoOfYearExperience = request.NoOfYearExperience,

            InstituteName = request.InstituteName.Trim(),
            Subject = request.Subject.Trim(),

            ExpectedSalary = request.ExpectedSalary,
            SalaryCurrency = string.IsNullOrWhiteSpace(request.SalaryCurrency) ? "BDT" : request.SalaryCurrency.Trim()
        };

        _db.Candidates.Add(candidate);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = candidate.Id }, candidate);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertCandidateRequest request, CancellationToken ct)
    {
        var candidate = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (candidate is null) return NotFound();

        candidate.FullName = request.FullName.Trim();
        candidate.Email = request.Email.Trim();
        candidate.Phone = request.Phone.Trim();
        candidate.AddressLine = request.AddressLine.Trim();

        candidate.PreviousCompanyName = request.PreviousCompanyName?.Trim();
        candidate.NoOfYearExperience = request.NoOfYearExperience;

        candidate.InstituteName = request.InstituteName.Trim();
        candidate.Subject = request.Subject.Trim();

        candidate.ExpectedSalary = request.ExpectedSalary;
        candidate.SalaryCurrency = string.IsNullOrWhiteSpace(request.SalaryCurrency) ? "BDT" : request.SalaryCurrency.Trim();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/resume")]
    [RequestSizeLimit(10_000_000)] // 10MB
    public async Task<IActionResult> UploadResume(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("File is required.");

        var allowed = new[]
        {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

        if (!allowed.Contains(file.ContentType))
            return BadRequest("Only PDF, DOC, DOCX allowed.");

        var candidate = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (candidate is null) return NotFound();

        var tenantId = candidate.TenantId;

        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        var folder = Path.Combine(uploadsRoot, tenantId.ToString(), "candidates", id.ToString());
        Directory.CreateDirectory(folder);

        var safeFileName = Path.GetFileName(file.FileName);
        var savedPath = Path.Combine(folder, safeFileName);

        await using (var stream = System.IO.File.Create(savedPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var urlPath = $"/uploads/{tenantId}/candidates/{id}/{Uri.EscapeDataString(safeFileName)}";

        candidate.ResumeFileName = safeFileName;
        candidate.ResumeContentType = file.ContentType;
        candidate.ResumeSize = file.Length;
        candidate.ResumeUrl = urlPath;

        await _db.SaveChangesAsync(ct);

        return Ok(new { candidate.Id, candidate.ResumeUrl });
    }

}
