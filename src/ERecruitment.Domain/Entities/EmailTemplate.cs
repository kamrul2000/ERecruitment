using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class EmailTemplate : BaseEntity
{
    // Example: "Rejection", "InterviewInvite", "Shortlist", "Offer"
    public string TemplateType { get; set; } = default!;

    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!; // plain text or HTML
    public bool IsEnabled { get; set; } = true;
}
