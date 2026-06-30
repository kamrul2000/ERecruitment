using ERecruitment.API.DTOs.Offers;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Admin / Recruiter / HiringManager (read); mutations are Admin/Recruiter
public sealed class OffersController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _currentUser;

    public OffersController(IApplicationDbContext db, IAuditLogger audit, ICurrentUser currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    // GET: api/offers/get-by-application/{appId}
    [HttpGet("get-by-application/{appId:guid}")]
    public async Task<IActionResult> GetByApplication(Guid appId, CancellationToken ct)
    {
        var offers = await _db.Offers.AsNoTracking()
            .Where(o => o.JobApplicationId == appId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return Ok(offers);
    }

    // POST: api/offers — create a Draft offer for an application.
    [HttpPost]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Create([FromBody] CreateOfferRequest req, CancellationToken ct)
    {
        if (req.JobApplicationId == Guid.Empty) return BadRequest("JobApplicationId required.");
        if (string.IsNullOrWhiteSpace(req.PositionTitle)) return BadRequest("PositionTitle required.");

        var app = await _db.JobApplications.FirstOrDefaultAsync(x => x.Id == req.JobApplicationId, ct);
        if (app is null) return NotFound("Application not found (or not in this tenant).");

        var offer = new Offer
        {
            JobApplicationId = app.Id,
            CandidateId = app.CandidateId,
            JobPostingId = app.JobPostingId,
            PositionTitle = req.PositionTitle.Trim(),
            Salary = req.Salary,
            SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? "BDT" : req.SalaryCurrency.Trim(),
            StartDate = req.StartDate,
            ExpiresAt = req.ExpiresAt,
            Notes = req.Notes?.Trim(),
            Status = "Draft",
            CreatedByEmail = _currentUser.Email
        };

        _db.Offers.Add(offer);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Offer.Created", "Offer", offer.Id,
            $"Created offer for {offer.PositionTitle}",
            new { offer.Id, offer.JobApplicationId, offer.PositionTitle }, ct);

        return CreatedAtAction(nameof(GetByApplication), new { appId = offer.JobApplicationId }, offer);
    }

    // PUT: api/offers/{id} — edit while still Draft.
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOfferRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.PositionTitle)) return BadRequest("PositionTitle required.");

        var offer = await _db.Offers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (offer is null) return NotFound();
        if (offer.Status != "Draft") return BadRequest("Only draft offers can be edited.");

        offer.PositionTitle = req.PositionTitle.Trim();
        offer.Salary = req.Salary;
        offer.SalaryCurrency = string.IsNullOrWhiteSpace(req.SalaryCurrency) ? "BDT" : req.SalaryCurrency.Trim();
        offer.StartDate = req.StartDate;
        offer.ExpiresAt = req.ExpiresAt;
        offer.Notes = req.Notes?.Trim();

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Offer.Updated", "Offer", offer.Id, "Updated draft offer", new { offer.Id }, ct);
        return NoContent();
    }

    // PUT: api/offers/{id}/send — Draft -> Sent.
    [HttpPut("{id:guid}/send")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (offer is null) return NotFound();
        if (offer.Status != "Draft") return BadRequest("Only draft offers can be sent.");

        offer.Status = "Sent";
        offer.SentAt = DateTimeOffset.UtcNow;
        offer.CreatedByEmail ??= _currentUser.Email;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Offer.Sent", "Offer", offer.Id, $"Sent offer for {offer.PositionTitle}", new { offer.Id }, ct);
        return NoContent();
    }

    // PUT: api/offers/{id}/accept — Sent -> Accepted; moves the application to Hired.
    [HttpPut("{id:guid}/accept")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] RespondOfferRequest? req, CancellationToken ct)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (offer is null) return NotFound();
        if (offer.Status != "Sent") return BadRequest("Only sent offers can be accepted.");

        offer.Status = "Accepted";
        offer.RespondedAt = DateTimeOffset.UtcNow;
        offer.ResponseNote = req?.ResponseNote?.Trim();

        // Accepting an offer advances the application to Hired (recorded in history).
        var app = await _db.JobApplications.FirstOrDefaultAsync(x => x.Id == offer.JobApplicationId, ct);
        if (app is not null && !string.Equals(app.Status, "Hired", StringComparison.OrdinalIgnoreCase))
        {
            var from = app.Status;
            app.Status = "Hired";
            _db.JobApplicationStatusHistories.Add(new JobApplicationStatusHistory
            {
                JobApplicationId = app.Id,
                FromStatus = from,
                ToStatus = "Hired",
                Comment = "Offer accepted",
                ChangedBy = _currentUser.Email ?? _currentUser.UserId?.ToString()
            });
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Offer.Accepted", "Offer", offer.Id, "Offer accepted",
            new { offer.Id, offer.JobApplicationId }, ct);
        return NoContent();
    }

    // PUT: api/offers/{id}/decline — Sent -> Declined.
    [HttpPut("{id:guid}/decline")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Decline(Guid id, [FromBody] RespondOfferRequest? req, CancellationToken ct)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (offer is null) return NotFound();
        if (offer.Status != "Sent") return BadRequest("Only sent offers can be declined.");

        offer.Status = "Declined";
        offer.RespondedAt = DateTimeOffset.UtcNow;
        offer.ResponseNote = req?.ResponseNote?.Trim();

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Offer.Declined", "Offer", offer.Id, "Offer declined", new { offer.Id }, ct);
        return NoContent();
    }

    // PUT: api/offers/{id}/withdraw — Draft|Sent -> Withdrawn.
    [HttpPut("{id:guid}/withdraw")]
    [Authorize(Roles = "Admin,Recruiter")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
    {
        var offer = await _db.Offers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (offer is null) return NotFound();
        if (offer.Status is not ("Draft" or "Sent"))
            return BadRequest("Only draft or sent offers can be withdrawn.");

        offer.Status = "Withdrawn";
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Offer.Withdrawn", "Offer", offer.Id, "Offer withdrawn", new { offer.Id }, ct);
        return NoContent();
    }
}
