namespace ERecruitment.Domain.Common;

public abstract class BaseEntity : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
}
