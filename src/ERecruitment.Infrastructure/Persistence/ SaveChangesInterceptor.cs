using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERecruitment.Infrastructure.Persistence;

public sealed class TenantAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTime _clock;

    public TenantAuditSaveChangesInterceptor(ITenantProvider tenantProvider, IDateTime clock)
    {
        _tenantProvider = tenantProvider;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTenantAndAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantAndAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenantAndAudit(DbContext? context)
    {
        if (context is null) return;

        // Skip tenant auditing if no tenant is set
        if (!_tenantProvider.HasTenant) return;

        var now = _clock.UtcNow;
        var tenantId = _tenantProvider.GetTenantId();

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Prevent tenant switching
                entry.Property(x => x.TenantId).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
