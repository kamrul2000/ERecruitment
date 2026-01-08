namespace ERecruitment.API.DTOs.Applications;

public sealed class ApplicationFilterQuery
{
    // Filters
    public string? Status { get; set; }           // "Submitted" or "Submitted,Reviewed"
    public string? Keyword { get; set; }          // name/email/phone
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public int? MinExperienceYears { get; set; }
    public int? MaxExperienceYears { get; set; }

    // Paging
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Sorting
    public string SortBy { get; set; } = "createdAt"; // createdAt | salary | experience
    public string SortOrder { get; set; } = "desc";   // asc | desc
}
