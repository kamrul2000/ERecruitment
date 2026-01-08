using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public class JobApplication : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Guid JobPostingId { get; set; }

    // Submitted / Reviewed / Shortlisted / Rejected / Hired
    public string Status { get; set; } = "Submitted";

    public string? Notes { get; set; }

    public decimal? ExpectedSalary { get; set; }
    public string SalaryCurrency { get; set; } = "BDT";

    // NEW: CV snapshot (important)
    public string? ResumeUrlSnapshot { get; set; }
    public string? ResumeFileNameSnapshot { get; set; }
    public string? ResumeContentTypeSnapshot { get; set; }
    public long? ResumeSizeSnapshot { get; set; }
}
