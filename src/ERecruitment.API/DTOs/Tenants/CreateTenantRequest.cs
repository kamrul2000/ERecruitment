namespace ERecruitment.API.DTOs.Tenants;

public sealed class CreateTenantRequest
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
}
