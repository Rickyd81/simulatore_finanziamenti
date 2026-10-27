using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Controllers;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// I servizi di hosting (es. Render) indicano la porta da usare nella variabile PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } porta)
    builder.WebHost.UseUrls($"http://0.0.0.0:{porta}");

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Il contratto OpenAPI è pubblico (/openapi/v1.json): descrive anche il tracciato standard verso il core banking.
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((documento, _, _) =>
{
    documento.Info.Title = "Simulatore finanziamenti";
    documento.Info.Description = "API del portale: simulazione, fabbrica prodotti e richiesta di finanziamento verso il core banking.";
    return Task.CompletedTask;
}));
builder.Services.AddDbContext<SimulatoreDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Simulatore") ?? "Data Source=simulatore.db"));
builder.Services.AddSingleton<MotoreSimulazione>();
builder.Services.AddSingleton(TimeProvider.System);
// Sostituire con l'adattatore del core banking reale quando sarà disponibile.
builder.Services.AddSingleton<ICoreBanking, CoreBankingSimulato>();

var origini = builder.Configuration.GetSection("Cors:Origini").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origini).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "simulatore.banca";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        // È un'API: rispondere 401/403 invece di reindirizzare a una pagina di login.
        o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });

var areaBancaAperta = builder.Environment.IsDevelopment() && string.IsNullOrEmpty(builder.Configuration["AreaBanca:Password"]);
builder.Services.AddAuthorizationBuilder().AddPolicy(AccessoController.Politica, p =>
{
    if (areaBancaAperta) p.RequireAssertion(_ => true);
    else p.RequireAuthenticatedUser();
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("accesso", c => RateLimitPartition.GetFixedWindowLimiter(
        c.Connection.RemoteIpAddress?.ToString() ?? "",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// Dietro il proxy del servizio di hosting lo schema originale (https) arriva negli header X-Forwarded-*.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SimulatoreDbContext>();
    db.Database.EnsureCreated();
    DatiIniziali.Carica(db);
}

app.UseForwardedHeaders();
// In produzione il front-end Angular compilato è in wwwroot e viene servito dalla stessa applicazione.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapOpenApi();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
