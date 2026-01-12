namespace ERecruitment.Infrastructure.Email;

public sealed class SmtpOptions
{
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 587;
    public string User { get; set; } = default!;
    public string Pass { get; set; } = default!;
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = "ERecruitment";
    public bool UseStartTls { get; set; } = true;
}
