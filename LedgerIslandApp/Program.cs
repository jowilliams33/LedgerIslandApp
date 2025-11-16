using LedgerIslandApp.Components;
using LedgerIslandApp.Data;
using LedgerIslandApp.Endpoints;
using LedgerIslandApp.Hubs;
using LedgerIslandApp.Models.Auth;
using LedgerIslandApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -------- Services --------

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

// Persist DataProtection keys so cookies remain valid across restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")))
    .SetApplicationName("LedgerIslandApp");

// Allow external auth round-trip cookies
builder.Services.Configure<CookiePolicyOptions>(o =>
{
    o.MinimumSameSitePolicy = SameSiteMode.None;
    o.Secure = CookieSecurePolicy.Always;
});

// ---------- Custom Services (CONCRETE ONLY) ----------
builder.Services.AddSingleton<TrnOcrService>();
builder.Services.AddScoped<CsvParserService>();
builder.Services.AddScoped<DataCleaningService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<LoggerService>();

// Keep ONLY this interface — required for Google login + session logic


// ---------- Auth (Cookies + Google) ----------
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddGoogle(googleOptions =>
    {
        var cfg = builder.Configuration;
        googleOptions.ClientId = cfg["Authentication:Google:ClientId"]!;
        googleOptions.ClientSecret = cfg["Authentication:Google:ClientSecret"]!;
        googleOptions.CallbackPath = "/signin-google";

        googleOptions.CorrelationCookie.SameSite = SameSiteMode.None;
        googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        googleOptions.CorrelationCookie.Path = "/";

        googleOptions.Scope.Add("email");
        googleOptions.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Email, "email");

        googleOptions.Events.OnCreatingTicket = async ctx =>
        {
            var email = ctx.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                        ?? ctx.Principal.Identity?.Name ?? "unknown";
            var name = ctx.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var sub = ctx.Principal.FindFirst("sub")?.Value;

            var sessionService = ctx.HttpContext.RequestServices.GetRequiredService<ISessionService>();
            var ua = ctx.Request.Headers.UserAgent.ToString();
            var ip = ctx.HttpContext.Connection.RemoteIpAddress?.ToString();

            var (sessionId, rawToken) = await sessionService.CreateAsync(
                userId: email, ua: ua, ip: ip,
                invalidateOthers: true, ttl: TimeSpan.FromHours(12),
                ct: ctx.HttpContext.RequestAborted);

            ctx.HttpContext.Response.Cookies.Append("sid", rawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(12)
            });

            var controlSecret = ctx.HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Auth:ControlSecret"]!;
            var ctl = ControlKey.Make(controlSecret, sessionId);

            ctx.HttpContext.Response.Cookies.Append("ctl", ctl, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(12)
            });

            ctx.HttpContext.Response.Cookies.Append("sid_id", sessionId.ToString("N"), new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(12)
            });

            await sessionService.AuditAsync(new LoginAudit
            {
                UserId = email,
                Provider = "google",
                LoggedAt = DateTime.UtcNow,
                IpAddress = ip,
                UserAgent = ua,
                Success = true,
                ProviderId = sub,
                Email = email,
                Name = name
            }, ctx.HttpContext.RequestAborted);
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 512L * 1024 * 1024; // 512 MB uploads
});

builder.Services.AddSignalR(o =>
{
    o.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
});

// Razor Components
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LedgerIsland API v1");
    c.RoutePrefix = "swagger";
});

// -------- Pipeline --------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.Use(async (ctx, next) =>
{
    var p = ctx.Request.Path.Value ?? "";
    if (p.Equals("/login", StringComparison.OrdinalIgnoreCase) ||
        p.Equals("/signin-google", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.Headers["Blazor-Disable-Enhanced-Navigation"] = "true";
    }
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// -------- UI & Endpoints --------
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapGet("/login", async ctx =>
{
    var props = new AuthenticationProperties
    {
        RedirectUri = "/imports"
    };
    props.Items["prompt"] = "select_account";
    await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
});

app.MapGet("/auth/google", async ctx =>
{
    await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
});

// ---------- FIXED ENDPOINTS (NO INTERFACES) ----------
app.MapPost("/imports/{batchId:guid}/post", async (Guid batchId, ImportService svc) =>
{
    await svc.PostToGoldenAsync(batchId);
    return Results.Ok(new { batchId, posted = true });
});

app.MapGet("/imports/{batchId:guid}/issues", async (Guid batchId, ValidationService svc) =>
{
    var issues = await svc.GetIssuesAsync(batchId);
    return Results.Ok(issues);
});

app.MapPost("/logout", async ctx =>
{
    await ctx.SignOutAsync();
    ctx.Response.Cookies.Delete("sid");
    ctx.Response.Cookies.Delete("ctl");
    ctx.Response.Cookies.Delete("sid_id");
    ctx.Response.Redirect("/");
});

app.MapPost("/ocr/trn", async (IFormFile file, TrnOcrService ocr) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file uploaded");

    await using var s = file.OpenReadStream();
    var (value, confidence) = await ocr.ExtractTrnAsync(s);

    return value is null
        ? Results.NotFound(new { message = "TRN not found" })
        : Results.Ok(new { value, confidence });
})
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status200OK)
.DisableAntiforgery();

app.MapHub<SyncHub>("/sync");
ControlEndpoints.MapControl(app);

app.Run();
