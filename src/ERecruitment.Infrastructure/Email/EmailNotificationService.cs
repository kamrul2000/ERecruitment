using ERecruitment.Application.Abstractions;
using ERecruitment.Application.Services;
using ERecruitment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.Infrastructure.Email;

public sealed class EmailNotificationService : IEmailNotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _sender;

    public EmailNotificationService(IApplicationDbContext db, IEmailSender sender)
    {
        _db = db;
        _sender = sender;
    }

    // TemplateTypes (you can keep these as constants)
    private const string ApplicationReceived = "ApplicationReceived";
    private const string StatusChanged = "StatusChanged"; // generic
    private const string InterviewScheduled = "InterviewScheduled";
    private const string InterviewCancelled = "InterviewCancelled";
    private const string InterviewReminder = "InterviewReminder";
    // Or per-status: Status.Shortlisted / Status.Rejected / Status.Hired...

    public async Task SendApplicationReceivedAsync(Guid applicationId, CancellationToken ct)
    {
        var app = await LoadApplicationData(applicationId, ct);
        if (app is null) return;

        await SendUsingTemplate(
            templateType: ApplicationReceived,
            toEmail: app.CandidateEmail,
            values: BuildValues(app, newStatus: app.Status, notes: app.Notes),
            relatedId: applicationId,
            ct: ct
        );
    }

    public async Task SendStatusChangedAsync(Guid applicationId, string newStatus, string? notes, CancellationToken ct)
    {
        var app = await LoadApplicationData(applicationId, ct);
        if (app is null) return;

        // Option 1 (simple): one template for all statuses
        await SendUsingTemplate(
            templateType: StatusChanged,
            toEmail: app.CandidateEmail,
            values: BuildValues(app, newStatus, notes),
            relatedId: applicationId,
            ct: ct
        );

        // Option 2 (advanced): per-status template (uncomment if you prefer)
        // await SendUsingTemplate($"Status.{newStatus}", app.CandidateEmail, BuildValues(app, newStatus, notes), applicationId, ct);
    }

    public async Task SendInterviewScheduledAsync(Guid interviewId, CancellationToken ct)
    {
        var v = await LoadInterviewData(interviewId, ct);
        if (v is null) return;

        await SendUsingTemplate(
            templateType: InterviewScheduled,
            toEmail: v.CandidateEmail,
            values: BuildInterviewValues(v),
            relatedId: v.ApplicationId, // ties the email to the application's history
            ct: ct
        );
    }

    public async Task SendInterviewCancelledAsync(Guid interviewId, CancellationToken ct)
    {
        var v = await LoadInterviewData(interviewId, ct);
        if (v is null) return;

        await SendUsingTemplate(
            templateType: InterviewCancelled,
            toEmail: v.CandidateEmail,
            values: BuildInterviewValues(v),
            relatedId: v.ApplicationId,
            ct: ct
        );
    }

    public async Task SendInterviewReminderAsync(Guid interviewId, CancellationToken ct)
    {
        var v = await LoadInterviewData(interviewId, ct);
        if (v is null) return;

        await SendUsingTemplate(
            templateType: InterviewReminder,
            toEmail: v.CandidateEmail,
            values: BuildInterviewValues(v),
            relatedId: v.ApplicationId,
            ct: ct
        );
    }

    private static Dictionary<string, string> BuildInterviewValues(InterviewView v)
    {
        return new Dictionary<string, string>
        {
            ["CandidateName"] = v.CandidateName,
            ["CandidateEmail"] = v.CandidateEmail,
            ["JobTitle"] = v.JobTitle,
            ["InterviewDate"] = v.StartsAtUtc.ToString("f"),
            ["Mode"] = v.Mode ?? "",
            ["Location"] = v.Location ?? "",
            ["MeetingLink"] = v.MeetingLink ?? ""
        };
    }

    private async Task<InterviewView?> LoadInterviewData(Guid interviewId, CancellationToken ct)
    {
        return await (
            from i in _db.Interviews.AsNoTracking()
            join a in _db.JobApplications.AsNoTracking() on i.JobApplicationId equals a.Id
            join c in _db.Candidates.AsNoTracking() on a.CandidateId equals c.Id
            join j in _db.JobPostings.AsNoTracking() on a.JobPostingId equals j.Id
            where i.Id == interviewId
            select new InterviewView
            {
                ApplicationId = a.Id,
                CandidateName = c.FullName,
                CandidateEmail = c.Email,
                JobTitle = j.Title,
                StartsAtUtc = i.StartsAtUtc,
                Mode = i.Mode,
                Location = i.Location,
                MeetingLink = i.MeetingLink
            }
        ).FirstOrDefaultAsync(ct);
    }

    private sealed class InterviewView
    {
        public Guid ApplicationId { get; set; }
        public string CandidateName { get; set; } = default!;
        public string CandidateEmail { get; set; } = default!;
        public string JobTitle { get; set; } = default!;
        public DateTimeOffset StartsAtUtc { get; set; }
        public string? Mode { get; set; }
        public string? Location { get; set; }
        public string? MeetingLink { get; set; }
    }

    private async Task SendUsingTemplate(
        string templateType,
        string toEmail,
        Dictionary<string, string> values,
        Guid relatedId,
        CancellationToken ct)
    {
        var tpl = await _db.EmailTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TemplateType == templateType && x.IsEnabled, ct);

        if (tpl is null) return; // template not configured => skip

        var settings = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is not null)
        {
            values["CompanyName"] = settings.CompanyName;
            values["PrimaryColor"] = settings.PrimaryColor;
        }

        var subject = EmailTemplateRenderer.Render(tpl.Subject, values);
        var body = EmailTemplateRenderer.Render(tpl.Body, values);

        var log = new EmailLog
        {
            ToEmail = toEmail,
            TemplateType = templateType,
            Subject = subject,
            Body = body,
            Status = "Sent",
            RelatedId = relatedId
        };

        try
        {
            await _sender.SendAsync(toEmail, subject, body, isHtml: false, ct);
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.Error = ex.Message;
        }

        _db.EmailLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    private Dictionary<string, string> BuildValues(AppView app, string newStatus, string? notes)
    {
        return new Dictionary<string, string>
        {
            ["CandidateName"] = app.CandidateName,
            ["CandidateEmail"] = app.CandidateEmail,
            ["JobTitle"] = app.JobTitle,
            ["Department"] = app.Department ?? "",
            ["Status"] = newStatus ?? "",
            ["Notes"] = notes ?? ""
        };
    }

    private async Task<AppView?> LoadApplicationData(Guid applicationId, CancellationToken ct)
    {
        return await (
            from a in _db.JobApplications.AsNoTracking()
            join c in _db.Candidates.AsNoTracking() on a.CandidateId equals c.Id
            join j in _db.JobPostings.AsNoTracking() on a.JobPostingId equals j.Id
            where a.Id == applicationId
            select new AppView
            {
                ApplicationId = a.Id,
                Status = a.Status,
                Notes = a.Notes,
                CandidateName = c.FullName,
                CandidateEmail = c.Email,
                JobTitle = j.Title,
                Department = j.Department
            }
        ).FirstOrDefaultAsync(ct);
    }

    private sealed class AppView
    {
        public Guid ApplicationId { get; set; }
        public string Status { get; set; } = default!;
        public string? Notes { get; set; }

        public string CandidateName { get; set; } = default!;
        public string CandidateEmail { get; set; } = default!;

        public string JobTitle { get; set; } = default!;
        public string? Department { get; set; }
    }
}
