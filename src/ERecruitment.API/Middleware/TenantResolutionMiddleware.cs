using System.Security.Claims;
using ERecruitment.Infrastructure.Tenancy;

namespace ERecruitment.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Allow swagger and tenant creation without tenant header/token
        if (path.StartsWith("/swagger") || path.StartsWith("/api/tenants"))
            return next(context);

        var provider = context.RequestServices.GetRequiredService<TenantProvider>();

        // 1) Try tenantId from JWT claim (after authentication)
        var claimTenant = context.User?.FindFirst("tenantId")?.Value;
        if (!string.IsNullOrWhiteSpace(claimTenant) && Guid.TryParse(claimTenant, out var jwtTenantId))
        {
            provider.SetTenant(jwtTenantId);
            return next(context);
        }

        // 2) Fallback: header (for login/register/dev)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
            Guid.TryParse(tenantHeader.ToString(), out var headerTenantId))
        {
            provider.SetTenant(headerTenantId);
            return next(context);
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Missing tenant. Provide JWT with tenantId claim or X-Tenant-Id header.");
    }
}
