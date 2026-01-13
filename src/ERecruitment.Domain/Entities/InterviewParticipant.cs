using ERecruitment.Domain.Common;

namespace ERecruitment.Domain.Entities;

public sealed class InterviewParticipant : BaseEntity
{
    public Guid InterviewId { get; set; }
    public Guid UserId { get; set; }   // AppUser.Id

    public string Role { get; set; } = "Interviewer"; // Interviewer/Observer
}
