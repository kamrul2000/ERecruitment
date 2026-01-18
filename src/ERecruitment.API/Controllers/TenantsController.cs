using ERecruitment.API.DTOs.Tenants;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERecruitment.API.Security;


namespace ERecruitment.API.Controllers;

[Authorize(Roles = "SuperAdmin")]

[ApiController]
[Route("api/[controller]")]
public sealed class TenantsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher = new();


    public TenantsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenants = await _db.Tenants.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.IsActive,
                t.Plan,
                t.BillingEmail,

                Users = _db.Users.IgnoreQueryFilters().Count(u => u.TenantId == t.Id),
                Jobs = _db.JobPostings.Count(j => j.TenantId == t.Id),
                Applications = _db.JobApplications.Count(a => a.TenantId == t.Id)
            })
            .ToListAsync(ct);

        return Ok(tenants);
    }



    [HttpPost("create-with-admin")]
    public async Task<IActionResult> CreateWithAdmin([FromBody] CreateTenantWithAdminRequest req, CancellationToken ct)
    {
        // validate
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");
        if (string.IsNullOrWhiteSpace(req.Slug)) return BadRequest("Slug required");
        if (string.IsNullOrWhiteSpace(req.AdminEmail)) return BadRequest("AdminEmail required");
        if (string.IsNullOrWhiteSpace(req.AdminPassword)) return BadRequest("AdminPassword required");

        var slug = req.Slug.Trim().ToLowerInvariant();
        var exists = await _db.Tenants.AnyAsync(x => x.Slug == slug, ct);
        if (exists) return Conflict("Slug already exists.");

        // 1) create tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Slug = slug,
            IsActive = true,
            BillingEmail = string.IsNullOrWhiteSpace(req.BillingEmail) ? req.AdminEmail.Trim() : req.BillingEmail.Trim(),
            Plan = string.IsNullOrWhiteSpace(req.Plan) ? "Free" : req.Plan.Trim()
        };
        _db.Tenants.Add(tenant);

        // 2) create first tenant admin user
        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FullName = req.AdminFullName.Trim(),
            Email = req.AdminEmail.Trim().ToLowerInvariant(),
            Role = "Admin",
            IsActive = true
        };

        // 🔹 Correctly hash password using the user instance
        admin.PasswordHash = _hasher.HashPassword(admin, req.AdminPassword);

        _db.Users.Add(admin);

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            AdminUserId = admin.Id,
            admin.Email
        });
    }

    [HttpPut("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
    {
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        t.IsActive = false;
        t.DisabledAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct)
    {
        var t = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();

        t.IsActive = true;
        t.DisabledAt = null;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

}
