using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public class JobPosting : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Department { get; set; } = default!;
    public string? Location { get; set; }
    public string? Description { get; set; }

    // Draft / Published / Closed
    public string Status { get; set; } = "Draft";
}
