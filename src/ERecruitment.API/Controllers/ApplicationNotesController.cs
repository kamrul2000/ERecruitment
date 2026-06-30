using ERecruitment.API.DTOs.Notes;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ApplicationNotesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _currentUser;

    public ApplicationNotesController(IApplicationDbContext db, IAuditLogger audit, ICurrentUser currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    // GET: api/applicationnotes/get-by-application/{appId}
    [HttpGet("get-by-application/{appId:guid}")]
    public async Task<IActionResult> GetByApplication(Guid appId, CancellationToken ct)
    {
        var notes = await _db.ApplicationNotes.AsNoTracking()
            .Where(n => n.JobApplicationId == appId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return Ok(notes);
    }

    // POST: api/applicationnotes — add a note or a scorecard.
    [HttpPost]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    public async Task<IActionResult> Create([FromBody] CreateNoteRequest req, CancellationToken ct)
    {
        if (req.JobApplicationId == Guid.Empty) return BadRequest("JobApplicationId required.");
        if (string.IsNullOrWhiteSpace(req.Body)) return BadRequest("Body required.");

        var appExists = await _db.JobApplications.AnyAsync(x => x.Id == req.JobApplicationId, ct);
        if (!appExists) return NotFound("Application not found (or not in this tenant).");

        var kind = string.Equals(req.Kind, "Scorecard", StringComparison.OrdinalIgnoreCase) ? "Scorecard" : "Note";
        static int? Clamp(int? v) => v.HasValue ? Math.Clamp(v.Value, 1, 5) : (int?)null;

        var note = new ApplicationNote
        {
            JobApplicationId = req.JobApplicationId,
            AuthorUserId = _currentUser.UserId,
            AuthorEmail = _currentUser.Email,
            Kind = kind,
            Body = req.Body.Trim(),
            TechnicalScore = kind == "Scorecard" ? Clamp(req.TechnicalScore) : null,
            CommunicationScore = kind == "Scorecard" ? Clamp(req.CommunicationScore) : null,
            CultureFitScore = kind == "Scorecard" ? Clamp(req.CultureFitScore) : null,
            Recommendation = kind == "Scorecard" ? req.Recommendation?.Trim() : null
        };

        _db.ApplicationNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync($"Application.{kind}Added", "ApplicationNote", note.Id,
            $"{kind} added", new { note.Id, note.JobApplicationId, note.Kind }, ct);

        return CreatedAtAction(nameof(GetByApplication), new { appId = note.JobApplicationId }, note);
    }

    // DELETE: api/applicationnotes/{id} — author or Admin only.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var note = await _db.ApplicationNotes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (note is null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && note.AuthorUserId != _currentUser.UserId)
            return Forbid();

        _db.ApplicationNotes.Remove(note);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Application.NoteDeleted", "ApplicationNote", id, "Note deleted", new { id }, ct);
        return NoContent();
    }
}
