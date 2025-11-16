namespace LedgerIslandApp.Services;

using LedgerIslandApp.Data;
using LedgerIslandApp.Hubs;
using LedgerIslandApp.Models.Auth;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

public sealed class SessionService(AppDbContext db, IHubContext<SyncHub> hub) : ISessionService
{
    static byte[] Hash(string raw) => SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    static string NewRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);

    public async Task AuditAsync(LedgerIslandApp.Models.Auth.LoginAudit a, CancellationToken ct)
    {
        db.LoginAudits.Add(a);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(Guid, string)> CreateAsync(
        string userId,
        string? ua,
        string? ip,
        bool invalidateOthers,
        TimeSpan? ttl,
        CancellationToken ct)
    {
        var raw = NewRawToken();
        var now = DateTime.UtcNow;

        if (invalidateOthers)
        {
            // collect other active sessions first
            var others = await db.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .Select(s => s.SessionId)
                .ToListAsync(ct);

            // mark them inactive
            await db.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);

            // notify those sessions to logout now
            foreach (var sid in others)
                await hub.Clients.Group(sid.ToString("N")).SendAsync("ForceLogout");
        }

        var entity = new Session
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            Token = Hash(raw),
            CreatedAt = now,
            LastSeenAt = now,
            BrowserInfo = ua,
            IpAddress = ip,
            IsActive = true,
            ExpiresAt = ttl.HasValue ? now.Add(ttl.Value) : null
        };

        db.Sessions.Add(entity);
        await db.SaveChangesAsync(ct);
        return (entity.SessionId, raw);
    }

    public async Task<bool> ValidateAsync(string rawToken, string? ip, string? ua, CancellationToken ct)
    {
        var tokenHash = Hash(rawToken);
        var now = DateTime.UtcNow;

        var session = await db.Sessions
            .Where(s => s.IsActive && s.Token == tokenHash && (s.ExpiresAt == null || s.ExpiresAt > now))
            .FirstOrDefaultAsync(ct);

        if (session is null) return false;

        // 15-min inactivity check
        if (session.LastSeenAt.HasValue && now - session.LastSeenAt.Value > IdleTimeout)
        {
            await db.Sessions
                .Where(s => s.SessionId == session.SessionId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);
            return false;
        }

        // slide activity window
        session.LastSeenAt = now;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task InvalidateAsync(Guid sessionId, CancellationToken ct)
    {
        await db.Sessions
            .Where(s => s.SessionId == sessionId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);
    }

    public async Task KickOthersAsync(string userId, Guid keepSessionId, CancellationToken ct)
    {
        await db.Sessions
            .Where(s => s.UserId == userId && s.SessionId != keepSessionId && s.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);

        // optional: push logout to others too
        var others = await db.Sessions
            .Where(s => s.UserId == userId && s.SessionId != keepSessionId)
            .Select(s => s.SessionId)
            .ToListAsync(ct);

        foreach (var sid in others)
            await hub.Clients.Group(sid.ToString("N")).SendAsync("ForceLogout");
    }
}
