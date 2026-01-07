using ERecruitment.Application.Abstractions;

namespace ERecruitment.Infrastructure.Tenancy
{
    public sealed class TenantProvider : ITenantProvider
    {
        private Guid? _tenantId;

        public bool HasTenant => _tenantId.HasValue;

        public void SetTenant(Guid tenantId) => _tenantId = tenantId;

        public Guid GetTenantId()
            => _tenantId ?? throw new InvalidOperationException("TenantId not resolved.");
    }
}
