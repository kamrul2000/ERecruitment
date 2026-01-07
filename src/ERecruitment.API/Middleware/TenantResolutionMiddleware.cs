using ERecruitment.Infrastructure.Persistence;
using ERecruitment.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
namespace ERecruitment.API.Middleware
{
    public sealed class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, TenantProvider tenantProvider)
        {
            var path = context.Request.Path.Value ?? "";

            // Skip tenant resolution for tenant creation endpoints
            if (path.StartsWith("/api/tenants", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing or invalid X-Tenant-Id header.");
                return;
            }

            // Check tenant exists in DB and is active
            var tenantExists = await dbContext.Tenants
                .AsNoTracking()
                .AnyAsync(t => t.Id == tenantId && t.IsActive);

            if (!tenantExists)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Tenant not found or inactive.");
                return;
            }

            // Set the tenant in TenantProvider
            tenantProvider.SetTenant(tenantId);

            await _next(context);
        }
    }
}