using ERecruitment.Application.Abstractions;

namespace ERecruitment.Infrastructure.Time;

public sealed class SystemDateTime : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
