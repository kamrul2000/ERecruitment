namespace ERecruitment.API.Contracts.Candidates;

public sealed class CandidateRequest
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
}
