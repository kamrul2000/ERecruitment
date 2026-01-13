using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class Interview : BaseEntity
{
    public Guid JobApplicationId { get; set; }
    public Guid InterviewRoundId { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;

    public string Mode { get; set; } = "Online"; // Online / Onsite / Phone
    public string? Location { get; set; }        // physical location or "Google Meet"
    public string? MeetingLink { get; set; }

    // Scheduled / Completed / Cancelled / NoShow
    public string Status { get; set; } = "Scheduled";

    public string? Notes { get; set; }
}
