using ERecruitment.API.DTOs.Audit;
using ERecruitment.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public AuditLogsController(IApplicationDbContext db) => _db = db;

    // GET: /api/AuditLogs?from=2026-01-01&to=2026-01-31&action=...&entityType=...&keyword=...&page=1&pageSize=20
    //[HttpGet]
    //public async Task<IActionResult> Search(
    //    [FromQuery] DateTimeOffset? from,
    //    [FromQuery] DateTimeOffset? to,
    //    [FromQuery] string? action,
    //    [FromQuery] string? entityType,
    //    [FromQuery] Guid? entityId,
    //    [FromQuery] Guid? actorUserId,
    //    [FromQuery] string? keyword,
    //    [FromQuery] int page = 1,
    //    [FromQuery] int pageSize = 20,
    //    CancellationToken ct = default)
    //{
    //    page = page <= 0 ? 1 : page;
    //    pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

    //    var q = _db.AuditLogs.AsNoTracking();

    //    if (from is not null) q = q.Where(x => x.CreatedAt >= from);
    //    if (to is not null) q = q.Where(x => x.CreatedAt <= to);

    //    if (!string.IsNullOrWhiteSpace(action))
    //        q = q.Where(x => x.Action.Contains(action.Trim()));

    //    if (!string.IsNullOrWhiteSpace(entityType))
    //        q = q.Where(x => x.EntityType == entityType.Trim());

    //    if (entityId is not null)
    //        q = q.Where(x => x.EntityId == entityId);

    //    if (actorUserId is not null)
    //        q = q.Where(x => x.ActorUserId == actorUserId);

    //    if (!string.IsNullOrWhiteSpace(keyword))
    //    {
    //        var k = keyword.Trim();
    //        q = q.Where(x =>
    //            (x.Summary != null && x.Summary.Contains(k)) ||
    //            (x.ActorEmail != null && x.ActorEmail.Contains(k)) ||
    //            (x.Action != null && x.Action.Contains(k)) ||
    //            (x.EntityType != null && x.EntityType.Contains(k)));
    //    }

    //    var total = await q.CountAsync(ct);

    //    var items = await q
    //        .OrderByDescending(x => x.CreatedAt)
    //        .Skip((page - 1) * pageSize)
    //        .Take(pageSize)
    //        .Select(x => new
    //        {
    //            x.Id,
    //            x.TenantId,
    //            x.CreatedAt,

    //            x.Action,
    //            x.EntityType,
    //            x.EntityId,

    //            x.ActorUserId,
    //            x.ActorEmail,
    //            x.ActorRole,

    //            x.Summary,
    //            x.IpAddress
    //        })
    //        .ToListAsync(ct);

    //    return Ok(new { total, page, pageSize, items });
    //}

    [HttpPost("search")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Search(
    [FromBody] AuditLogSearchRequest req,
    CancellationToken ct = default)
    {
        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = req.PageSize <= 0 ? 20 : Math.Min(req.PageSize, 100);

        var q = _db.AuditLogs.AsNoTracking();

        if (req.From is not null) q = q.Where(x => x.CreatedAt >= req.From);
        if (req.To is not null) q = q.Where(x => x.CreatedAt <= req.To);

        if (!string.IsNullOrWhiteSpace(req.Action))
            q = q.Where(x => x.Action.Contains(req.Action.Trim()));

        if (!string.IsNullOrWhiteSpace(req.EntityType))
            q = q.Where(x => x.EntityType == req.EntityType.Trim());

        if (req.EntityId is not null)
            q = q.Where(x => x.EntityId == req.EntityId);

        if (req.ActorUserId is not null)
            q = q.Where(x => x.ActorUserId == req.ActorUserId);

        if (!string.IsNullOrWhiteSpace(req.Keyword))
        {
            var k = req.Keyword.Trim();
            q = q.Where(x =>
                (x.Summary != null && x.Summary.Contains(k)) ||
                (x.ActorEmail != null && x.ActorEmail.Contains(k)) ||
                (x.Action != null && x.Action.Contains(k)) ||
                (x.EntityType != null && x.EntityType.Contains(k)));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.CreatedAt,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.ActorUserId,
                x.ActorEmail,
                x.ActorRole,
                x.Summary,
                x.IpAddress
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }


    // GET: /api/AuditLogs/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _db.AuditLogs.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.CreatedAt,

                x.Action,
                x.EntityType,
                x.EntityId,

                x.ActorUserId,
                x.ActorEmail,
                x.ActorRole,

                x.Summary,
                x.DataJson,
                x.IpAddress,
                x.UserAgent
            })
            .FirstOrDefaultAsync(ct);

        if (item is null) return NotFound();
        return Ok(item);
    }
}
