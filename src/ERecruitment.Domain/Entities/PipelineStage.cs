using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class PipelineStage : BaseEntity
{
    public string Name { get; set; } = default!;         // e.g. Submitted
    public string Key { get; set; } = default!;          // stable key e.g. submitted
    public int SortOrder { get; set; }                   // 1..n
    public bool IsActive { get; set; } = true;
    public bool IsTerminal { get; set; } = false;        // Rejected/Hired
}
