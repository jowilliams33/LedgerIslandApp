using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using LedgerIslandApp.Services;

namespace LedgerIslandApp.Endpoints;

public static class ControlEndpoints
{
    public static void MapControl(this IEndpointRouteBuilder app)
    {
        // Kick all other sessions for a user, keep one
        app.MapPost("/control/kick-others", async (HttpContext ctx, SessionService sessions) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<KickDto>();
            if (body is null) return Results.BadRequest();

            await sessions.KickOthersAsync(body.UserId, body.KeepSessionId, ctx.RequestAborted);
            return Results.Ok(new { ok = true });
        });
    }

    private record KickDto(string UserId, Guid KeepSessionId);
}
