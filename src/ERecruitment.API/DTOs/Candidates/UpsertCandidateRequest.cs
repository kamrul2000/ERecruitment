namespace ERecruitment.API.DTOs.Candidates;

public sealed class UpsertCandidateRequest
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string AddressLine { get; set; } = default!;

    public string? PreviousCompanyName { get; set; }
    public int? NoOfYearExperience { get; set; }

    public string InstituteName { get; set; } = default!;
    public string Subject { get; set; } = default!;

    public decimal? ExpectedSalary { get; set; }
    public string SalaryCurrency { get; set; } = "BDT";
}
