using ERecruitment.API.DTOs.Communications;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CommunicationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _sender;
    private readonly IAuditLogger _audit;

    public CommunicationsController(IApplicationDbContext db, IEmailSender sender, IAuditLogger audit)
    {
        _db = db;
        _sender = sender;
        _audit = audit;
    }

    // GET: api/communications/get-by-application/{appId}
    // Full email history for an application (auto + ad-hoc), newest first.
    [HttpGet("get-by-application/{appId:guid}")]
    public async Task<IActionResult> GetByApplication(Guid appId, CancellationToken ct)
    {
        var logs = await _db.EmailLogs.AsNoTracking()
            .Where(x => x.RelatedId == appId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.ToEmail,
                x.TemplateType,
                x.Subject,
                x.Body,
                x.Status,
                x.Error,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(logs);
    }

    // POST: api/communications/send — ad-hoc email to the application's candidate.
    [HttpPost("send")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Send([FromBody] SendEmailRequest req, CancellationToken ct)
    {
        if (req.JobApplicationId == Guid.Empty) return BadRequest("JobApplicationId required.");
        if (string.IsNullOrWhiteSpace(req.Subject)) return BadRequest("Subject required.");
        if (string.IsNullOrWhiteSpace(req.Body)) return BadRequest("Body required.");

        // Tenant-scoped lookup of the recipient.
        var data = await (
            from a in _db.JobApplications.AsNoTracking()
            join c in _db.Candidates.AsNoTracking() on a.CandidateId equals c.Id
            where a.Id == req.JobApplicationId
            select new { a.Id, c.Email }
        ).FirstOrDefaultAsync(ct);

        if (data is null) return NotFound("Application not found (or not in this tenant).");

        var log = new EmailLog
        {
            ToEmail = data.Email,
            TemplateType = "Custom",
            Subject = req.Subject.Trim(),
            Body = req.Body,
            Status = "Sent",
            RelatedId = data.Id
        };

        try
        {
            await _sender.SendAsync(data.Email, log.Subject, log.Body, isHtml: false, ct);
        }
        catch (Exception ex)
        {
            // The email is still recorded so the failure is visible in the history.
            log.Status = "Failed";
            log.Error = ex.Message;
        }

        _db.EmailLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Communication.EmailSent", "JobApplication", data.Id,
            $"Email to {data.Email}: {log.Subject}",
            new { data.Id, data.Email, log.Subject, log.Status }, ct);

        return Ok(new { log.Id, log.Status, log.Error });
    }
}
