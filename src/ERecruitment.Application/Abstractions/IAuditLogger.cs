namespace ERecruitment.Application.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        string? summary = null,
        object? data = null,
        CancellationToken ct = default);
}
