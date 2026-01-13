using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class InterviewFeedback : BaseEntity
{
    public Guid InterviewId { get; set; }
    public Guid ReviewerUserId { get; set; } // who gave feedback (AppUser.Id)

    // StrongHire / Hire / LeanHire / NoHire / StrongNoHire
    public string Decision { get; set; } = "Hire";

    public int Rating { get; set; } = 4; // 1..5
    public string? Comments { get; set; }

    public bool IsSubmitted { get; set; } = false;
}
