using System.Text.Json;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace ERecruitment.Infrastructure.Auditing;

public sealed class AuditLogger : IAuditLogger
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _http;

    public AuditLogger(IApplicationDbContext db, ICurrentUser currentUser, IHttpContextAccessor http)
    {
        _db = db;
        _currentUser = currentUser;
        _http = http;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        string? summary = null,
        object? data = null,
        CancellationToken ct = default)
    {
        // TenantId must exist for tenant isolation
        var tenantId = _currentUser.TenantId;
        if (tenantId is null) return; // for safety (anonymous public endpoints can set tenant context if needed)

        var ctx = _http.HttpContext;

        var log = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,

            ActorUserId = _currentUser.UserId,
            ActorEmail = _currentUser.Email,
            ActorRole = _currentUser.Role,

            Summary = summary,
            DataJson = data is null ? null : JsonSerializer.Serialize(data),

            IpAddress = ctx?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = ctx?.Request?.Headers["User-Agent"].ToString()
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
