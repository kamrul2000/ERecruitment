using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class InterviewRound : BaseEntity
{
    public Guid JobApplicationId { get; set; }

    public string Name { get; set; } = "Round 1";  // e.g. Technical Round
    public int SortOrder { get; set; } = 1;

    // Planned / InProgress / Completed / Cancelled
    public string Status { get; set; } = "Planned";
}
