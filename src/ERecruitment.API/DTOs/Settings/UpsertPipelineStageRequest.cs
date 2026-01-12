namespace ERecruitment.API.DTOs.Settings;

public sealed class UpsertPipelineStageRequest
{
    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTerminal { get; set; } = false;
}
