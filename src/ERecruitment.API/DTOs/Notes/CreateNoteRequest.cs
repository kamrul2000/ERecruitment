namespace ERecruitment.API.DTOs.Notes;

public sealed class CreateNoteRequest
{
    public Guid JobApplicationId { get; set; }
    public string Kind { get; set; } = "Note"; // Note | Scorecard
    public string Body { get; set; } = default!;

    // Scorecard-only fields (1..5).
    public int? TechnicalScore { get; set; }
    public int? CommunicationScore { get; set; }
    public int? CultureFitScore { get; set; }
    public string? Recommendation { get; set; }
}
