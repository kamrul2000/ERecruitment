using ERecruitment.Application.Abstractions;
using ERecruitment.Infrastructure.Persistence;
using ERecruitment.Infrastructure.Tenancy;
using ERecruitment.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERecruitment.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<TenantProvider>();
        services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<TenantProvider>());

        services.AddSingleton<IDateTime, SystemDateTime>();
        services.AddScoped<TenantAuditSaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var cs = config.GetConnectionString("DefaultConnection");
            options.UseSqlServer(cs);

            options.AddInterceptors(sp.GetRequiredService<TenantAuditSaveChangesInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
