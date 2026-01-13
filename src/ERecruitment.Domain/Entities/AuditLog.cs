using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public string Action { get; set; } = default!;          // e.g. "Application.StatusChanged"
    public string EntityType { get; set; } = default!;      // e.g. "JobApplication"
    public Guid? EntityId { get; set; }                     // entity primary key

    public Guid? ActorUserId { get; set; }                  // logged-in user id (JWT sub)
    public string? ActorEmail { get; set; }
    public string? ActorRole { get; set; }

    public string? Summary { get; set; }                    // short text for UI
    public string? DataJson { get; set; }                   // optional details

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
