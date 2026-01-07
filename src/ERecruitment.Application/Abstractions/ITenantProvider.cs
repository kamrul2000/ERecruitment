namespace ERecruitment.Application.Abstractions;

public interface ITenantProvider
{
    Guid GetTenantId();
    bool HasTenant { get; }
}
