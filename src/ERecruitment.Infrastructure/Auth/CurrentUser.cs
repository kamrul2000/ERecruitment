using System.Security.Claims;
using ERecruitment.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ERecruitment.Infrastructure.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst("email")?.Value;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

    public Guid? TenantId
    {
        get
        {
            // you used "tenantId" claim in your token
            var tid = User?.FindFirst("tenantId")?.Value;
            return Guid.TryParse(tid, out var id) ? id : null;
        }
    }
}
