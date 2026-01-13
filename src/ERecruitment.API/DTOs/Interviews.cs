namespace ERecruitment.API.DTOs.Interviews;

public sealed class CreateRoundRequest
{
    public Guid JobApplicationId { get; set; }
    public string Name { get; set; } = "Round 1";
    public int SortOrder { get; set; } = 1;
}

public sealed class ScheduleInterviewRequest
{
    public Guid JobApplicationId { get; set; }
    public Guid InterviewRoundId { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;

    public string Mode { get; set; } = "Online";
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public string? Notes { get; set; }

    public List<Guid> ParticipantUserIds { get; set; } = new();
}

public sealed class SubmitFeedbackRequest
{
    public int Rating { get; set; } = 4;
    public string Decision { get; set; } = "Hire";
    public string? Comments { get; set; }
    public bool IsSubmitted { get; set; } = true;
}
