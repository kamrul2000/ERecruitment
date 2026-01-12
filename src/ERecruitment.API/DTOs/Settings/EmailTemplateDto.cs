namespace ERecruitment.API.DTOs.Settings;

public sealed class EmailTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateType { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public bool IsEnabled { get; set; }
}
