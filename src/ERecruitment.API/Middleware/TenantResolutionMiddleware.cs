using ERecruitment.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;

namespace ERecruitment.API.Middleware;

public sealed class TenantResolutionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // 🔹 Check endpoint metadata FIRST
        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null;

        if (allowAnonymous || context.Request.Path.StartsWithSegments("/api/public"))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Allow swagger + auth + tenants without tenant context
        if (path.StartsWith("/swagger") ||
            path.StartsWith("/api/auth") ||
            path.StartsWith("/api/tenants"))
        {
            await next(context);
            return;
        }

        var tenantClaim = context.User?.FindFirst("tenantId")?.Value;

        if (!string.IsNullOrWhiteSpace(tenantClaim) &&
            Guid.TryParse(tenantClaim, out var tenantId))
        {
            var provider = context.RequestServices.GetRequiredService<TenantProvider>();
            provider.SetTenant(tenantId);

            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Missing tenantId claim. Login required.");
    }
}
