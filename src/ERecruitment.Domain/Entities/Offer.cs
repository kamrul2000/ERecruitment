using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

// An employment offer extended to a candidate for a specific application.
// Lifecycle: Draft -> Sent -> Accepted | Declined | Withdrawn | Expired.
public sealed class Offer : BaseEntity
{
    public Guid JobApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobPostingId { get; set; }

    public string PositionTitle { get; set; } = default!;

    public decimal? Salary { get; set; }
    public string SalaryCurrency { get; set; } = "BDT";

    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    // Offer letter body / internal notes.
    public string? Notes { get; set; }

    // Draft / Sent / Accepted / Declined / Withdrawn / Expired
    public string Status { get; set; } = "Draft";

    public string? CreatedByEmail { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public string? ResponseNote { get; set; }
}
