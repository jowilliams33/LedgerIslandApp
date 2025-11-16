using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace LedgerIslandApp.Hubs;

public class SyncHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext()!;
        var sidHex = http.Request.Query["sid_id"].ToString();
        var ctl = http.Request.Query["ctl"].ToString();
        if (!Guid.TryParseExact(sidHex, "N", out var sid)) { Context.Abort(); return; }

        var secret = http.RequestServices.GetRequiredService<IConfiguration>()["Auth:ControlSecret"]!;
        if (!ControlKey.Verify(secret, sid, ctl)) { Context.Abort(); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, sidHex);
        await base.OnConnectedAsync();
    }
}
