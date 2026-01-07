namespace ERecruitment.Application.Abstractions;

public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
}
