using ERecruitment.API;
using ERecruitment.API.Extensions;
using ERecruitment.API.Middleware;
using ERecruitment.Infrastructure.DependencyInjection;
using ERecruitment.Infrastructure.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithTenantHeader();

// Infrastructure services (DbContext, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Tenant provider for middleware
builder.Services.AddScoped<TenantProvider>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERecruitment API v1"));

app.UseHttpsRedirection();

// Tenant resolution middleware
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseStaticFiles();

app.MapControllers();
app.Run();
