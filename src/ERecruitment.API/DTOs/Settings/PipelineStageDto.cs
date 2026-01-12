namespace ERecruitment.API.DTOs.Settings;

public sealed class PipelineStageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsTerminal { get; set; }
}
