using System.Linq.Expressions;
using ERecruitment.Application.Abstractions;
using ERecruitment.Domain.Common;
using ERecruitment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERecruitment.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantProvider _tenantProvider;

    // EF parameterizes this per request scope. When no tenant is set
    // (e.g. SuperAdmin requests, or pre-auth requests), return Guid.Empty
    // so tenant-filtered queries match no rows by default. SuperAdmin
    // endpoints that need cross-tenant access must use IgnoreQueryFilters().
    public Guid CurrentTenantId =>
        _tenantProvider.HasTenant ? _tenantProvider.GetTenantId() : Guid.Empty;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Candidate> Candidates { get; set; }
    public DbSet<JobPosting> JobPostings { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<JobApplicationStatusHistory> JobApplicationStatusHistories { get; set; }
    public DbSet<AppUser>Users { get; set; }

    public DbSet<TenantSettings> TenantSettings { get; set; }
    public DbSet<PipelineStage> PipelineStages { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<InterviewRound> InterviewRounds => Set<InterviewRound>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewParticipant> InterviewParticipants => Set<InterviewParticipant>();
    public DbSet<InterviewFeedback> InterviewFeedbacks => Set<InterviewFeedback>();
    public DbSet<TenantThemeSettings> TenantThemeSettings => Set<TenantThemeSettings>();
    public DbSet<Offer> Offers => Set<Offer>();


    //IQueryable<JobPosting> IApplicationDbContext.JobPostings => Jobs.AsQueryable();
    //IQueryable<Candidate> IApplicationDbContext.Candidates => Candidates.AsQueryable();
    //IQueryable<JobApplication> IApplicationDbContext.JobApplications => JobApplications.AsQueryable();
    //IQueryable<Tenant> IApplicationDbContext.Tenants => TenantsSet.AsQueryable();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Decimal precision FIX
        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.Property(x => x.ExpectedSalary)
                  .HasPrecision(18, 2);
        });

        modelBuilder.Entity<JobApplication>()
            .Property(x => x.ExpectedSalary)
            .HasPrecision(18, 2);

        // Apply global tenant filter to every entity that implements ITenantEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType is null) continue;

            if (typeof(ITenantEntity).IsAssignableFrom(clrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);

                method.Invoke(this, new object[] { modelBuilder });
            }
        }

        // Example: Candidate email unique per tenant
        modelBuilder.Entity<Candidate>()
            .HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique();

        // Optional: keep Tenants outside tenant filter (do not implement ITenantEntity)
        modelBuilder.Entity<Tenant>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<JobPosting>()
    .HasIndex(x => new { x.TenantId, x.Title });

        modelBuilder.Entity<JobApplication>()
            .HasIndex(x => new { x.TenantId, x.CandidateId, x.JobPostingId })
            .IsUnique();

        // Optional FK relations (recommended)
        modelBuilder.Entity<JobApplication>()
            .HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(x => x.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobApplication>()
            .HasOne<JobPosting>()
            .WithMany()
            .HasForeignKey(x => x.JobPostingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobApplicationStatusHistory>()
    .HasIndex(x => new { x.TenantId, x.JobApplicationId, x.CreatedAt });

        modelBuilder.Entity<JobApplicationStatusHistory>()
            .HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(x => x.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<AppUser>()
    .HasIndex(x => new { x.TenantId, x.Email })
    .IsUnique();

        modelBuilder.Entity<TenantSettings>()
    .HasIndex(x => x.TenantId)
    .IsUnique();

        modelBuilder.Entity<PipelineStage>()
            .HasIndex(x => new { x.TenantId, x.Key })
            .IsUnique();

        modelBuilder.Entity<EmailTemplate>()
            .HasIndex(x => new { x.TenantId, x.TemplateType })
            .IsUnique();
        modelBuilder.Entity<AuditLog>()
    .HasIndex(x => new { x.TenantId, x.CreatedAt });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId });
        modelBuilder.Entity<InterviewRound>()
    .HasIndex(x => new { x.TenantId, x.JobApplicationId, x.SortOrder });

        modelBuilder.Entity<Interview>()
            .HasIndex(x => new { x.TenantId, x.JobApplicationId, x.StartsAtUtc });

        modelBuilder.Entity<InterviewParticipant>()
            .HasIndex(x => new { x.TenantId, x.InterviewId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<InterviewFeedback>()
            .HasIndex(x => new { x.TenantId, x.InterviewId, x.ReviewerUserId })
            .IsUnique();
        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Email);

        modelBuilder.Entity<AppUser>()
            .HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique(false); // you can make unique per-tenant later

        modelBuilder.Entity<TenantThemeSettings>()
    .HasIndex(x => x.TenantId)
    .IsUnique();

        modelBuilder.Entity<Offer>()
            .Property(x => x.Salary)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Offer>()
            .HasIndex(x => new { x.TenantId, x.JobApplicationId });

        modelBuilder.Entity<Offer>()
            .HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(x => x.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);



    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        // e => e.TenantId == CurrentTenantId
        Expression<Func<TEntity, bool>> filter = e => e.TenantId == CurrentTenantId;
        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}
