using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SimulatoreFinanziamenti.Api.Controllers;

public record RichiestaAccesso(string Password);

public record StatoAccesso(bool Autenticato, bool PasswordRichiesta);

/// <summary>
/// Accesso all'area banca con una password unica (configurazione AreaBanca:Password).
/// Senza password configurata l'area banca è aperta solo in ambiente di sviluppo.
/// </summary>
[ApiController]
[Route("api/accesso")]
public class AccessoController(IConfiguration configurazione, IHostEnvironment ambiente) : ControllerBase
{
    public const string Politica = "AreaBanca";

    private string? Password => configurazione["AreaBanca:Password"] is { Length: > 0 } p ? p : null;

    [HttpGet]
    public StatoAccesso Stato() => new(
        User.Identity?.IsAuthenticated == true || (Password is null && ambiente.IsDevelopment()),
        Password is not null || !ambiente.IsDevelopment());

    [HttpPost]
    [EnableRateLimiting("accesso")]
    public async Task<IActionResult> Entra(RichiestaAccesso richiesta)
    {
        if (Password is not { } attesa || !Uguali(richiesta.Password, attesa))
            return Problem(title: "Password non corretta", statusCode: StatusCodes.Status401Unauthorized);

        var identita = new ClaimsIdentity([new Claim(ClaimTypes.Name, "banca")], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identita));
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Esci()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private static bool Uguali(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(a)), SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}
