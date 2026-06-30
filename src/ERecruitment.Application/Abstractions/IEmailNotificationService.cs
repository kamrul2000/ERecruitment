namespace ERecruitment.Application.Abstractions;

public interface IEmailNotificationService
{
    Task SendApplicationReceivedAsync(Guid applicationId, CancellationToken ct);
    Task SendStatusChangedAsync(Guid applicationId, string newStatus, string? notes, CancellationToken ct);
    Task SendInterviewScheduledAsync(Guid interviewId, CancellationToken ct);
    Task SendInterviewCancelledAsync(Guid interviewId, CancellationToken ct);
}
