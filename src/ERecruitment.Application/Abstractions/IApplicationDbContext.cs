using Microsoft.EntityFrameworkCore;
using ERecruitment.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;


namespace ERecruitment.Application.Abstractions
{
    public interface IApplicationDbContext
    {
        //IQueryable<JobPosting> JobPostings { get; }
        //IQueryable<Candidate> Candidates { get; }
        //IQueryable<JobApplication> JobApplications { get; }
        //IQueryable<Tenant> Tenants { get; }

        DbSet<JobPosting> JobPostings { get; }
        DbSet<Candidate> Candidates { get; }
        DbSet<JobApplication> JobApplications { get; }
        DbSet<Tenant> Tenants { get; }
        DbSet<JobApplicationStatusHistory> JobApplicationStatusHistories { get; }
        DbSet<AppUser> Users { get; }
        DbSet<TenantSettings> TenantSettings { get; }
        DbSet<PipelineStage> PipelineStages { get; }
        DbSet<EmailTemplate> EmailTemplates { get; }

        DbSet<EmailLog> EmailLogs { get; set; }
        DbSet<AuditLog> AuditLogs { get; }

        DbSet<InterviewRound> InterviewRounds { get; }
        DbSet<Interview> Interviews { get; }
        DbSet<InterviewParticipant> InterviewParticipants { get; }
        DbSet<InterviewFeedback> InterviewFeedbacks { get; }


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
