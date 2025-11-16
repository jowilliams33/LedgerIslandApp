namespace LedgerIslandApp.Models.Auth;

public sealed class LoginAudit
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Provider { get; set; } = "local"; // "local" | "google"
    public DateTime LoggedAt { get; set; }          // UTC
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ProviderId { get; set; } // e.g. Google "sub"
    public string? Email { get; set; }
    public string? Name { get; set; }
}
