using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DisabledAt { get; set; }
    public string? BillingEmail { get; set; }
    public string Plan { get; set; } = "Free"; // Free/Pro/Enterprise
}