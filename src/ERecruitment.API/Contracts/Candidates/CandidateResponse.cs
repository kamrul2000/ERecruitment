namespace ERecruitment.API.Contracts.Candidates;

public sealed class CandidateResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;

    public string Phone { get; set; } = default;
    public int? NoOfYearExperience { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? ResumeFileName { get; set; }
    public string? ResumeContentType { get; set; }
    public long? ResumeSize { get; set; }
    public string? ResumeUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
