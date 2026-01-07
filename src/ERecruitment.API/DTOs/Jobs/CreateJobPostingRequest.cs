namespace ERecruitment.API.DTOs.Jobs;

public sealed class CreateJobPostingRequest
{
    public string Title { get; set; } = default!;
    public string Department { get; set; } = default!;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft"; // Draft/Published/Closed
}
