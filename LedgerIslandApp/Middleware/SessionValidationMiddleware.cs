using LedgerIslandApp.Services;

public sealed class SessionValidationMiddleware(RequestDelegate next, SessionService sessions)
{
    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";

        // Allowlist: static files, framework assets, login, Google auth/callback, home
        if (path.StartsWith("/_framework") || path.StartsWith("/_content") ||
            path.StartsWith("/css") || path.StartsWith("/img") ||
            path.Equals("/") || path.StartsWith("/login") ||
            path.StartsWith("/auth/google") || path.StartsWith("/signin-google"))
        {
            await next(ctx); return;
        }

        var token = ctx.Request.Cookies["sid"];
        if (string.IsNullOrEmpty(token))
        {
            ctx.Response.Redirect("/"); return;
        }

        var ok = await sessions.ValidateAsync(
            token,
            ctx.Connection.RemoteIpAddress?.ToString(),
            ctx.Request.Headers.UserAgent.ToString(),
            ctx.RequestAborted);

        if (!ok)
        {
            ctx.Response.Cookies.Delete("sid");
            ctx.Response.Redirect("/"); return;
        }

        await next(ctx);
    }
}
