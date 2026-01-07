namespace ERecruitment.Domain.Common;

public abstract class BaseEntity : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Tenant isolation
    public Guid TenantId { get; set; }

    // Auditing
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
