using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public class Candidate : BaseEntity
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
}