using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using LedgerIslandApp.Services;

namespace LedgerIslandApp.Endpoints;

public static class ControlEndpoints
{
    public static void MapControl(this IEndpointRouteBuilder app)
    {
        // Example endpoint: kick other sessions
        app.MapPost("/control/kick-others", async (HttpContext ctx, ISessionService sessions) =>
        {
            // In a real implementation, you'd validate auth & payload here.
            // For now, just parse JSON body with userId + keepSessionId.

            var body = await ctx.Request.ReadFromJsonAsync<KickDto>();
            if (body is null) return Results.BadRequest();

            await sessions.KickOthersAsync(body.UserId, body.KeepSessionId, ctx.RequestAborted);
            return Results.Ok(new { ok = true });
        });
    }

    private record KickDto(string UserId, Guid KeepSessionId);
}
