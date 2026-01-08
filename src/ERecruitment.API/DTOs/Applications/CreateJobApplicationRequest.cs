namespace ERecruitment.API.DTOs.Applications;

public sealed class CreateJobApplicationRequest
{
    
    public Guid CandidateId { get; set; }
    public Guid JobPostingId { get; set; }

    public decimal? ExpectedSalary { get; set; }
    public string SalaryCurrency { get; set; } = "BDT";

    public string? Notes { get; set; }
}
