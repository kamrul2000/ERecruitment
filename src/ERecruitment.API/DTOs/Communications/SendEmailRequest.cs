namespace ERecruitment.API.DTOs.Communications;

public sealed class SendEmailRequest
{
    public Guid JobApplicationId { get; set; }
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
}
