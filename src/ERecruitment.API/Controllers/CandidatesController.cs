using ERecruitment.API.Contracts.Candidates;
using ERecruitment.API.DTOs.Candidates;
using ERecruitment.API.Storage;
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

    // GET: api/candidates?page=&pageSize=&search=
    // Paginated + server-side search so the endpoint can never load an unbounded
    // result set. Returns { total, page, pageSize, items }.
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

        var query = _db.Candidates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var k = search.Trim();
            query = query.Where(x =>
                EF.Functions.Like(x.FullName, $"%{k}%") ||
                EF.Functions.Like(x.Email, $"%{k}%") ||
                EF.Functions.Like(x.Phone, $"%{k}%") ||
                EF.Functions.Like(x.AddressLine, $"%{k}%"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                AddressLine = x.AddressLine,

                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
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

    // Streams a candidate's CV through an authenticated, tenant-scoped endpoint.
    // The candidate query is tenant-filtered, so a caller from another tenant
    // simply gets 404. This replaces the previous anonymous static-file access.
    [HttpGet("{id:guid}/resume/file")]
    public async Task<IActionResult> DownloadResume(Guid id, CancellationToken ct)
    {
        var candidate = await _db.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (candidate is null) return NotFound();
        if (string.IsNullOrWhiteSpace(candidate.ResumeFileName))
            return NotFound("No resume on file.");

        var path = ResumeFiles.Resolve(candidate.TenantId, candidate.Id, candidate.ResumeFileName);
        if (path is null || !System.IO.File.Exists(path))
            return NotFound("Resume file missing.");

        var contentType = string.IsNullOrWhiteSpace(candidate.ResumeContentType)
            ? "application/octet-stream"
            : candidate.ResumeContentType;

        var stream = System.IO.File.OpenRead(path);
        // Inline disposition so PDFs preview in the browser; DOC/DOCX download.
        return File(stream, contentType, candidate.ResumeFileName, enableRangeProcessing: true);
    }

}
