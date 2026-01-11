using ERecruitment.API.DTOs.Users;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly PasswordHasher<AppUser> _hasher = new();

    private static readonly string[] AllowedRoles = ["Admin", "Recruiter", "HiringManager"];

    public UsersController(IApplicationDbContext db)
    {
        _db = db;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Users
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new UserResponse
            {
                Id = x.Id,
                TenantId = x.TenantId,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // GET: api/users/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UserResponse
            {
                Id = x.Id,
                TenantId = x.TenantId,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null) return NotFound();
        return Ok(item);
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest("FullName required.");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email required.");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password required.");

        var email = request.Email.Trim().ToLowerInvariant();
        var role = (request.Role ?? "Recruiter").Trim();

        if (!AllowedRoles.Contains(role))
            return BadRequest("Role must be Admin, Recruiter, or HiringManager.");

        var exists = await _db.Users.AnyAsync(x => x.Email == email, ct);
        if (exists) return Conflict("Email already exists in this tenant.");

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = role,
            IsActive = request.IsActive
        };

        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponse
        {
            Id = user.Id,
            TenantId = user.TenantId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    // PUT: api/users/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest("FullName required.");
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest("Email required.");

        var role = (request.Role ?? "Recruiter").Trim();
        if (!AllowedRoles.Contains(role))
            return BadRequest("Role must be Admin, Recruiter, or HiringManager.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound();

        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _db.Users.AnyAsync(x => x.Email == email && x.Id != id, ct);
        if (emailTaken) return Conflict("Email already exists in this tenant.");

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Role = role;
        user.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // PUT: api/users/{id}/reset-password
    [HttpPut("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("NewPassword required.");

        if (request.NewPassword.Trim().Length < 6)
            return BadRequest("Password must be at least 6 characters.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound();

        user.PasswordHash = _hasher.HashPassword(user, request.NewPassword.Trim());
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // PUT: api/users/{id}/toggle-active
    [HttpPut("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound();

        // Optional: prevent disabling yourself
        var currentUserId = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(currentUserId, out var me) && me == user.Id)
            return BadRequest("You cannot disable your own account.");

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/users/{id}  (Optional)
    // In SaaS, it's better to disable instead of delete.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
