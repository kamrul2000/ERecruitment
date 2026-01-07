using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public class JobApplication : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Guid JobPostingId { get; set; }

    // Submitted / Reviewed / Shortlisted / Rejected / Hired
    public string Status { get; set; } = "Submitted";

    public string? Notes { get; set; }
}
