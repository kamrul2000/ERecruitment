using ERecruitment.API.DTOs.Interviews;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Admin/Recruiter/HiringManager
public sealed class InterviewsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IEmailNotificationService _email; // optional but recommended

    public InterviewsController(IApplicationDbContext db, IAuditLogger audit, IEmailNotificationService email)
    {
        _db = db;
        _audit = audit;
        _email = email;
    }

    // GET: api/interviews/by-application/{appId}
    [HttpGet("get-by-application/{appId:guid}")]
    public async Task<IActionResult> GetByApplication(Guid appId, CancellationToken ct)
    {
        var rounds = await _db.InterviewRounds.AsNoTracking()
            .Where(r => r.JobApplicationId == appId)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(ct);

        var interviews = await _db.Interviews.AsNoTracking()
            .Where(i => i.JobApplicationId == appId)
            .OrderBy(i => i.StartsAtUtc)
            .ToListAsync(ct);

        var interviewIds = interviews.Select(x => x.Id).ToList();

        var participants = await _db.InterviewParticipants.AsNoTracking()
            .Where(p => interviewIds.Contains(p.InterviewId))
            .ToListAsync(ct);

        var feedbacks = await _db.InterviewFeedbacks.AsNoTracking()
            .Where(f => interviewIds.Contains(f.InterviewId))
            .ToListAsync(ct);

        return Ok(new { rounds, interviews, participants, feedbacks });
    }

    // POST: api/interviews/rounds
    [HttpPost("createRound")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    public async Task<IActionResult> CreateRound([FromBody] CreateRoundRequest req, CancellationToken ct)
    {
        if (req.JobApplicationId == Guid.Empty) return BadRequest("JobApplicationId required.");

        var round = new InterviewRound
        {
            JobApplicationId = req.JobApplicationId,
            Name = string.IsNullOrWhiteSpace(req.Name) ? "Round" : req.Name.Trim(),
            SortOrder = req.SortOrder <= 0 ? 1 : req.SortOrder,
            Status = "Planned"
        };

        _db.InterviewRounds.Add(round);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("InterviewRound.Created", "InterviewRound", round.Id,
            $"Created interview round {round.Name}", new { round.Id, round.JobApplicationId, round.Name }, ct);

        return Ok(round);
    }

    // POST: api/interviews/schedule
    [HttpPost("createSchedule")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    public async Task<IActionResult> Schedule([FromBody] ScheduleInterviewRequest req, CancellationToken ct)
    {
        if (req.JobApplicationId == Guid.Empty) return BadRequest("JobApplicationId required.");
        if (req.InterviewRoundId == Guid.Empty) return BadRequest("InterviewRoundId required.");

        var interview = new Interview
        {
            JobApplicationId = req.JobApplicationId,
            InterviewRoundId = req.InterviewRoundId,
            StartsAtUtc = req.StartsAtUtc,
            DurationMinutes = req.DurationMinutes <= 0 ? 60 : req.DurationMinutes,
            Mode = string.IsNullOrWhiteSpace(req.Mode) ? "Online" : req.Mode.Trim(),
            Location = req.Location?.Trim(),
            MeetingLink = req.MeetingLink?.Trim(),
            Notes = req.Notes
        };

        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync(ct);

        // participants
        var ids = (req.ParticipantUserIds ?? new List<Guid>()).Distinct().ToList();
        foreach (var uid in ids)
        {
            _db.InterviewParticipants.Add(new InterviewParticipant
            {
                InterviewId = interview.Id,
                UserId = uid,
                Role = "Interviewer"
            });
        }
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Interview.Scheduled", "Interview", interview.Id,
            $"Scheduled interview at {interview.StartsAtUtc:u}", new { interview.Id, interview.JobApplicationId, interview.StartsAtUtc }, ct);

        // Notify the candidate (logged against the application's communication history).
        await _email.SendInterviewScheduledAsync(interview.Id, ct);

        return Ok(new { interviewId = interview.Id });
    }

    // PUT: api/interviews/{id}/cancel
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var interview = await _db.Interviews.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (interview is null) return NotFound();

        interview.Status = "Cancelled";
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Interview.Cancelled", "Interview", interview.Id, "Cancelled interview", new { interview.Id }, ct);

        await _email.SendInterviewCancelledAsync(interview.Id, ct);
        return NoContent();
    }

    // PUT: api/interviews/{id}/complete
    [HttpPut("{id:guid}/complete")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var interview = await _db.Interviews.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (interview is null) return NotFound();

        interview.Status = "Completed";
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Interview.Completed", "Interview", interview.Id, "Completed interview", new { interview.Id }, ct);
        return NoContent();
    }
    [HttpPut("{id:guid}/feedback")]
    [Authorize(Roles = "Admin,Interviewer")]
    public async Task<IActionResult> SubmitFeedback(
        Guid id,
        [FromBody] SubmitFeedbackRequest req,
        CancellationToken ct)
    {
        var subClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(subClaim) || !Guid.TryParse(subClaim, out var reviewerId))
            return Unauthorized("Missing or invalid user claim.");

        var isAdmin = User.IsInRole("Admin");

        // Only non-admins must be interview participants
        if (!isAdmin)
        {
            var isParticipant = await _db.InterviewParticipants
                .AnyAsync(p => p.InterviewId == id && p.UserId == reviewerId, ct);

            if (!isParticipant)
                return Forbid("You are not a participant of this interview.");
        }

        var feedback = await _db.InterviewFeedbacks
            .FirstOrDefaultAsync(f => f.InterviewId == id && f.ReviewerUserId == reviewerId, ct);

        if (feedback == null)
        {
            feedback = new InterviewFeedback
            {
                InterviewId = id,
                ReviewerUserId = reviewerId
            };
            _db.InterviewFeedbacks.Add(feedback);
        }

        feedback.Rating = Math.Clamp(req.Rating, 1, 5);
        feedback.Decision = string.IsNullOrWhiteSpace(req.Decision) ? "Hire" : req.Decision.Trim();
        feedback.Comments = req.Comments;
        feedback.IsSubmitted = req.IsSubmitted;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Interview.FeedbackSubmitted",
            "InterviewFeedback",
            feedback.Id,
            $"Feedback submitted: {feedback.Decision} ({feedback.Rating}/5)",
            new { interviewId = id, feedback.Decision, feedback.Rating },
            ct
        );

        return NoContent();
    }



}
