namespace ERecruitment.API.DTOs.Applications;

public sealed class CreateJobApplicationRequest
{
    public Guid CandidateId { get; set; }
    public Guid JobPostingId { get; set; }
    public string? Notes { get; set; }
}
