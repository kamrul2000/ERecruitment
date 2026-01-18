using ERecruitment.Application.Abstractions;
using ERecruitment.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    private readonly IApplicationDbContext _db; // inject your DbContext

    public TenantResolutionMiddleware(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // 🔹 Check endpoint metadata FIRST
        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null;
        var role = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        // ✅ SuperAdmin bypasses tenant check
        if (role == "SuperAdmin")
        {
            await next(context);
            return;
        }

        // ✅ Public endpoints bypass tenant check
        if (allowAnonymous || context.Request.Path.StartsWithSegments("/api/public"))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // ✅ Allow swagger + auth + tenant endpoints without tenant context
        if (path.StartsWith("/swagger") ||
            path.StartsWith("/api/auth") ||
            path.StartsWith("/api/tenants"))
        {
            await next(context);
            return;
        }

        // 🔹 Extract tenantId from claim
        var tenantClaim = context.User?.FindFirst("tenantId")?.Value;

        if (!string.IsNullOrWhiteSpace(tenantClaim) &&
            Guid.TryParse(tenantClaim, out var tenantId))
        {
            // 🔹 New: validate tenant in DB
            var tenant = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, context.RequestAborted);

            if (tenant is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Tenant not found.");
                return;
            }

            if (!tenant.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Tenant is disabled.");
                return;
            }

            // 🔹 Set tenant in provider
            var provider = context.RequestServices.GetRequiredService<TenantProvider>();
            provider.SetTenant(tenantId);

            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Missing tenantId claim. Login required.");
    }
}
