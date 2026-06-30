using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

// An internal collaboration item on an application. Two kinds share one feed:
//  - "Note"      : free-text comment between recruiters/hiring managers.
//  - "Scorecard" : a structured evaluation (criteria scores + recommendation).
public sealed class ApplicationNote : BaseEntity
{
    public Guid JobApplicationId { get; set; }

    public Guid? AuthorUserId { get; set; }
    public string? AuthorEmail { get; set; }

    public string Kind { get; set; } = "Note"; // Note | Scorecard
    public string Body { get; set; } = default!;

    // Scorecard fields — null for plain notes. Each score is 1..5.
    public int? TechnicalScore { get; set; }
    public int? CommunicationScore { get; set; }
    public int? CultureFitScore { get; set; }
    public string? Recommendation { get; set; } // StrongYes / Yes / Neutral / No / StrongNo
}
