using ERecruitment.Infrastructure.Tenancy;

namespace ERecruitment.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Allow swagger + auth + tenants without tenant context
        if (path.StartsWith("/swagger") || path.StartsWith("/api/auth") || path.StartsWith("/api/tenants"))
            return next(context);

        var tenantClaim = context.User?.FindFirst("tenantId")?.Value;

        if (!string.IsNullOrWhiteSpace(tenantClaim) && Guid.TryParse(tenantClaim, out var tenantId))
        {
            var provider = context.RequestServices.GetRequiredService<TenantProvider>();
            provider.SetTenant(tenantId);
            return next(context);
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsync("Missing tenantId claim. Login required.");
    }
}
