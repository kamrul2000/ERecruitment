using ERecruitment.API.DTOs.Auth;
using ERecruitment.API.Security;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AuthController(IApplicationDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TenantSlug)) return BadRequest("TenantSlug required");
        if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest("FullName required");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email required");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password required");

        var slug = request.TenantSlug.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, ct);
        if (tenant is null) return BadRequest("Invalid tenant.");

        var role = (request.Role ?? "Recruiter").Trim();
        var allowedRoles = new[] { "Admin", "Recruiter", "HiringManager" };
        if (!allowedRoles.Contains(role))
            return BadRequest("Role must be Admin, Recruiter, or HiringManager.");

        var exists = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenant.Id && x.Email == email, ct);

        if (exists) return Conflict("User already exists in this tenant.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FullName = request.FullName.Trim(),
            Email = email,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Ok(new { user.Id, user.FullName, user.Email, user.Role, user.TenantId });
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TenantSlug)) return BadRequest("TenantSlug required");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email required");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password required");

        var slug = request.TenantSlug.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        // Find tenant first (Tenants are not tenant-filtered)
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, ct);
        if (tenant is null) return Unauthorized("Invalid tenant.");

        // IMPORTANT: Users are tenant-filtered, so we must query by TenantId manually.
        // If you have global query filter active, you need to temporarily bypass it here.
        // Use IgnoreQueryFilters() for login.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Email == email, ct);

        if (user is null) return Unauthorized("Invalid credentials.");
        if (!user.IsActive) return Unauthorized("User is inactive.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid credentials.");

        var token = _jwt.CreateToken(user);

        return Ok(new
        {
            accessToken = token,
            user = new { user.Id, user.FullName, user.Email, user.Role, user.TenantId },
            tenant = new { tenant.Id, tenant.Name, tenant.Slug }
        });
    }

}
