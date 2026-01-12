using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class EmailLog : BaseEntity
{
    public string ToEmail { get; set; } = default!;
    public string TemplateType { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string Status { get; set; } = "Sent"; // Sent/Failed
    public string? Error { get; set; }

    public Guid? RelatedId { get; set; } // applicationId
}
