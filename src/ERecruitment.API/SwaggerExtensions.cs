using Microsoft.OpenApi.Models;

namespace ERecruitment.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithTenantAndBearer(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ERecruitment API",
                Version = "v1"
            });

            // Tenant Header
            c.AddSecurityDefinition("TenantHeader", new OpenApiSecurityScheme
            {
                Name = "X-Tenant-Id",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Tenant Id (GUID)"
            });

            // Bearer JWT
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT token. Example: Bearer {your_token}"
            });

            // Require both (Swagger will send both if you fill both)
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "TenantHeader"
                        }
                    },
                    Array.Empty<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
