namespace ERecruitment.API.DTOs.Offers;

public sealed class CreateOfferRequest
{
    public Guid JobApplicationId { get; set; }
    public string PositionTitle { get; set; } = default!;
    public decimal? Salary { get; set; }
    public string? SalaryCurrency { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateOfferRequest
{
    public string PositionTitle { get; set; } = default!;
    public decimal? Salary { get; set; }
    public string? SalaryCurrency { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

public sealed class RespondOfferRequest
{
    public string? ResponseNote { get; set; }
}
