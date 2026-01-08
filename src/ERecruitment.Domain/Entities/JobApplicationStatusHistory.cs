using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public class JobApplicationStatusHistory : BaseEntity
{
    public Guid JobApplicationId { get; set; }

    public string FromStatus { get; set; } = default!;
    public string ToStatus { get; set; } = default!;

    public string? Comment { get; set; }

    // Later (Phase JWT) you can store user id/email here
    public string? ChangedBy { get; set; }
}
