namespace ERecruitment.API.DTOs.Applications;

public sealed class UpdateJobApplicationStatusRequest
{
    public string Status { get; set; } = default!; // Submitted/Reviewed/Shortlisted/Rejected/Hired
    public string? Notes { get; set; }
}
