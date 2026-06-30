using ERecruitment.API;
using ERecruitment.API.Extensions;
using ERecruitment.API.Middleware;
using ERecruitment.API.Security;
using ERecruitment.Application.Abstractions;
using ERecruitment.Infrastructure.DependencyInjection;
using ERecruitment.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ERecruitment.Infrastructure.Email;
using ERecruitment.Domain.Entities;
var builder = WebApplication.CreateBuilder(args);

// Fail-fast on missing critical configuration. Real values must come from
// User Secrets (dev) or environment variables (prod), never appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. " +
        "Set it via 'dotnet user-secrets' (dev) or environment variables (prod).");

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be configured and at least 32 characters. " +
        "Set it via 'dotnet user-secrets set \"Jwt:Key\" \"<long-random-value>\"' or environment variables.");

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithTenantAndBearer();
builder.Services.AddScoped<TenantResolutionMiddleware>();
builder.Services.AddHttpContextAccessor();

// Infrastructure services (DbContext, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Tenant provider for middleware
builder.Services.AddScoped<TenantProvider>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<ICurrentUser, ERecruitment.Infrastructure.Auth.CurrentUser>();
builder.Services.AddScoped<IAuditLogger, ERecruitment.Infrastructure.Auditing.AuditLogger>();

// Preserve JWT claim names ("sub", "email") instead of remapping them to legacy
// schema URIs. ICurrentUser reads "sub" directly.
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),

            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

// Default deny: every endpoint requires an authenticated user unless it
// explicitly opts out with [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", p =>
        p.WithOrigins("http://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod());
});


var app = builder.Build();
await ERecruitment.Infrastructure.Seeding.SeedData.SeedSuperAdminAsync(app);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERecruitment API v1"));

app.UseHttpsRedirection();

// Candidate CVs live under wwwroot/uploads/**/candidates/** but must NEVER be
// served as anonymous static files (that previously leaked resumes to anyone who
// guessed a URL). They are streamed only via the authenticated, tenant-scoped
// GET /api/Candidates/{id}/resume/file endpoint. Block any direct static hit on
// that path; tenant branding (logos/favicons) under /branding/ stays public.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path is not null
        && path.Contains("/uploads/", StringComparison.OrdinalIgnoreCase)
        && path.Contains("/candidates/", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseStaticFiles();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
