using ERecruitment.API;
using ERecruitment.API.Extensions;
using ERecruitment.API.Middleware;
using ERecruitment.Infrastructure.DependencyInjection;
using ERecruitment.Infrastructure.Tenancy;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ERecruitment.API.Security;

var builder = WebApplication.CreateBuilder(args);

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithTenantAndBearer();
builder.Services.AddScoped<TenantResolutionMiddleware>();

// Infrastructure services (DbContext, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Tenant provider for middleware
builder.Services.AddScoped<TenantProvider>();
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERecruitment API v1"));

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
// Tenant resolution middleware
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();


app.MapControllers();
app.Run();
