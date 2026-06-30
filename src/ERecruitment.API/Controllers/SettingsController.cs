using ERecruitment.API.DTOs.Settings;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class SettingsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public SettingsController(IApplicationDbContext db) => _db = db;

    // GET: api/settings
    [HttpGet("get-all")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var settings = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);

        // auto-create defaults for tenant if missing
        if (settings is null)
        {
            settings = new TenantSettings();
            _db.TenantSettings.Add(settings);
            await _db.SaveChangesAsync(ct);
        }

        var stages = await _db.PipelineStages
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => new PipelineStageDto
            {
                Id = x.Id,
                Name = x.Name,
                Key = x.Key,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsTerminal = x.IsTerminal
            })
            .ToListAsync(ct);

        // seed default stages if none
        if (stages.Count == 0)
        {
            var defaults = new[]
            {
                new PipelineStage { Name="Submitted", Key="submitted", SortOrder=1 },
                new PipelineStage { Name="Reviewed", Key="reviewed", SortOrder=2 },
                new PipelineStage { Name="Shortlisted", Key="shortlisted", SortOrder=3 },
                new PipelineStage { Name="Rejected", Key="rejected", SortOrder=99, IsTerminal=true },
                new PipelineStage { Name="Hired", Key="hired", SortOrder=100, IsTerminal=true }
            };

            _db.PipelineStages.AddRange(defaults);
            await _db.SaveChangesAsync(ct);

            stages = await _db.PipelineStages.AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .Select(x => new PipelineStageDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Key = x.Key,
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive,
                    IsTerminal = x.IsTerminal
                })
                .ToListAsync(ct);
        }

        var templates = await _db.EmailTemplates
            .AsNoTracking()
            .OrderBy(x => x.TemplateType)
            .Select(x => new EmailTemplateDto
            {
                Id = x.Id,
                TemplateType = x.TemplateType,
                Subject = x.Subject,
                Body = x.Body,
                IsEnabled = x.IsEnabled
            })
            .ToListAsync(ct);
        // Ensure all default templates exist. Idempotent per template type, so newly
        // introduced types (e.g. interview emails) get added even for existing tenants
        // that already had the original templates.
        var defaultTemplates = new[]
        {
            new EmailTemplate
            {
                TemplateType = "ApplicationReceived",
                Subject = "We received your application for {JobTitle}",
                Body =
        @"Hi {CandidateName},

Thanks for applying to {JobTitle} at {CompanyName}.
We will review your application and update you soon.

Regards,
{CompanyName}",
                IsEnabled = true
            },
            new EmailTemplate
            {
                TemplateType = "StatusChanged",
                Subject = "Update: {JobTitle} application status is now {Status}",
                Body =
        @"Hi {CandidateName},

Your application for {JobTitle} is now: {Status}.
Notes: {Notes}

Regards,
{CompanyName}",
                IsEnabled = true
            },
            new EmailTemplate
            {
                TemplateType = "InterviewScheduled",
                Subject = "Interview scheduled for {JobTitle}",
                Body =
        @"Hi {CandidateName},

Your interview for {JobTitle} at {CompanyName} is scheduled for {InterviewDate}.
Mode: {Mode}
Location: {Location}
Meeting link: {MeetingLink}

Good luck!
{CompanyName}",
                IsEnabled = true
            },
            new EmailTemplate
            {
                TemplateType = "InterviewCancelled",
                Subject = "Interview cancelled for {JobTitle}",
                Body =
        @"Hi {CandidateName},

Your interview for {JobTitle} at {CompanyName} scheduled for {InterviewDate} has been cancelled.
We will be in touch about next steps.

Regards,
{CompanyName}",
                IsEnabled = true
            },
            new EmailTemplate
            {
                TemplateType = "InterviewReminder",
                Subject = "Reminder: interview for {JobTitle} on {InterviewDate}",
                Body =
        @"Hi {CandidateName},

This is a reminder of your upcoming interview for {JobTitle} at {CompanyName}.
When: {InterviewDate}
Mode: {Mode}
Location: {Location}
Meeting link: {MeetingLink}

See you there!
{CompanyName}",
                IsEnabled = true
            }
        };

        var existingTypes = templates.Select(t => t.TemplateType).ToHashSet();
        var toAdd = defaultTemplates.Where(d => !existingTypes.Contains(d.TemplateType)).ToList();
        if (toAdd.Count > 0)
        {
            _db.EmailTemplates.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);

            // reload after insert
            templates = await _db.EmailTemplates
                .AsNoTracking()
                .OrderBy(x => x.TemplateType)
                .Select(x => new EmailTemplateDto
                {
                    Id = x.Id,
                    TemplateType = x.TemplateType,
                    Subject = x.Subject,
                    Body = x.Body,
                    IsEnabled = x.IsEnabled
                })
                .ToListAsync(ct);
        }

        return Ok(new
        {
            settings = new TenantSettingsResponse
            {
                TenantId = settings.TenantId,
                CompanyName = settings.CompanyName,
                LogoUrl = settings.LogoUrl,
                PrimaryColor = settings.PrimaryColor,
                CareerPageEnabled = settings.CareerPageEnabled,
                MaxResumeSizeMb = settings.MaxResumeSizeMb,
                AllowedResumeTypes = settings.AllowedResumeTypes,
                TimeZone = settings.TimeZone
            },
            pipelineStages = stages,
            emailTemplates = templates
        });
    }

    // PUT: api/settings
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UpdateTenantSettingsRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.CompanyName)) return BadRequest("CompanyName required.");
        if (string.IsNullOrWhiteSpace(req.PrimaryColor)) return BadRequest("PrimaryColor required.");

        var settings = await _db.TenantSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new TenantSettings();
            _db.TenantSettings.Add(settings);
        }

        settings.CompanyName = req.CompanyName.Trim();
        settings.LogoUrl = string.IsNullOrWhiteSpace(req.LogoUrl) ? null : req.LogoUrl.Trim();
        settings.PrimaryColor = req.PrimaryColor.Trim();
        settings.CareerPageEnabled = req.CareerPageEnabled;

        settings.MaxResumeSizeMb = req.MaxResumeSizeMb <= 0 ? 10 : req.MaxResumeSizeMb;
        settings.AllowedResumeTypes = string.IsNullOrWhiteSpace(req.AllowedResumeTypes) ? "pdf,doc,docx" : req.AllowedResumeTypes.Trim();
        settings.TimeZone = string.IsNullOrWhiteSpace(req.TimeZone) ? "Asia/Dhaka" : req.TimeZone.Trim();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST: api/settings/pipeline-stages
    [HttpPost("pipeline-stages/createStage")]
    public async Task<IActionResult> CreateStage([FromBody] UpsertPipelineStageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required.");
        if (string.IsNullOrWhiteSpace(req.Key)) return BadRequest("Key required.");

        var key = req.Key.Trim().ToLowerInvariant();

        var exists = await _db.PipelineStages.AnyAsync(x => x.Key == key, ct);
        if (exists) return Conflict("Stage key already exists.");

        var stage = new PipelineStage
        {
            Name = req.Name.Trim(),
            Key = key,
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            IsTerminal = req.IsTerminal
        };

        _db.PipelineStages.Add(stage);
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    // PUT: api/settings/pipeline-stages/{id}
    [HttpPut("pipeline-stages/updateStage/{id:guid}")]
    public async Task<IActionResult> UpdateStage(Guid id, [FromBody] UpsertPipelineStageRequest req, CancellationToken ct)
    {
        var stage = await _db.PipelineStages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (stage is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required.");
        if (string.IsNullOrWhiteSpace(req.Key)) return BadRequest("Key required.");

        var key = req.Key.Trim().ToLowerInvariant();

        var taken = await _db.PipelineStages.AnyAsync(x => x.Key == key && x.Id != id, ct);
        if (taken) return Conflict("Stage key already exists.");

        stage.Name = req.Name.Trim();
        stage.Key = key;
        stage.SortOrder = req.SortOrder;
        stage.IsActive = req.IsActive;
        stage.IsTerminal = req.IsTerminal;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // PUT: api/settings/pipeline-stages/{id}/toggle
    [HttpPut("pipeline-stages/updateToggleStage/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleStage(Guid id, CancellationToken ct)
    {
        var stage = await _db.PipelineStages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (stage is null) return NotFound();

        stage.IsActive = !stage.IsActive;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // PUT: api/settings/email-templates
    [HttpPut("email-templates/updateEmailTemplates")]
    public async Task<IActionResult> UpsertTemplate([FromBody] UpsertEmailTemplateRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.TemplateType)) return BadRequest("TemplateType required.");
        if (string.IsNullOrWhiteSpace(req.Subject)) return BadRequest("Subject required.");
        if (string.IsNullOrWhiteSpace(req.Body)) return BadRequest("Body required.");

        var type = req.TemplateType.Trim();

        var template = await _db.EmailTemplates.FirstOrDefaultAsync(x => x.TemplateType == type, ct);
        if (template is null)
        {
            template = new EmailTemplate { TemplateType = type };
            _db.EmailTemplates.Add(template);
        }

        template.Subject = req.Subject.Trim();
        template.Body = req.Body;
        template.IsEnabled = req.IsEnabled;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
