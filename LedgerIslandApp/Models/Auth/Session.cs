namespace LedgerIslandApp.Models.Auth;

public sealed class Session
{
    public Guid SessionId { get; set; }
    public string UserId { get; set; } = default!;
    public byte[] Token { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? BrowserInfo { get; set; }
    public string? IpAddress { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
