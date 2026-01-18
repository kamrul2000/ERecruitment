namespace ERecruitment.API.DTOs.Tenants;

public sealed class CreateTenantWithAdminRequest
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string AdminFullName { get; set; } = default!;
    public string AdminEmail { get; set; } = default!;
    public string AdminPassword { get; set; } = default!; // later: invite link instead
    public string? BillingEmail { get; set; }
    public string Plan { get; set; } = "Free";
}

