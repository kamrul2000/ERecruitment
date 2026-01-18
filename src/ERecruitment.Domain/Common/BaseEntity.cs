namespace ERecruitment.Domain.Common;

public abstract class BaseEntity : AuditableEntity
{
    public Guid TenantId { get; set; }
}
