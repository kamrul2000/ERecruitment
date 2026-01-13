namespace ERecruitment.API.DTOs.Audit
{
    public class AuditLogSearchRequest
    {
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }

        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }

        public Guid? ActorUserId { get; set; }
        public string? Keyword { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
