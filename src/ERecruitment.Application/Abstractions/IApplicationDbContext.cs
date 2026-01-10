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


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
