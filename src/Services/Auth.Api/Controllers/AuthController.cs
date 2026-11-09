using Auth.Api.Application.Dtos;
using Auth.Api.Application.Interfaces;
using Auth.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Auth.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    // La cookie solo se envia a las rutas /auth (no a /api/cargas)
    private const string RefreshCookieName = "refresh_token";
    private const string RefreshCookiePath = "/auth";
    private const string SessionExpiredMessage = "La sesión expiró. Inicie sesión nuevamente.";

    private readonly IAuthService _authService;
    private readonly bool _secureCookie;

    public AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _secureCookie = jwtOptions.Value.RefreshCookieSecure;
    }

    /// <summary>Valida las credenciales. Devuelve el JWT en el body y el refresh token en una cookie HttpOnly.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        if (!result.Succeeded)
            return Problem(title: "Correo o contraseña incorrectos.", statusCode: StatusCodes.Status401Unauthorized);

        SetRefreshCookie(result.Response!);
        return Ok(result.Response);
    }

    /// <summary>
    /// Entrega un nuevo JWT usando el refresh token de la cookie (que se rota en cada uso).
    /// El cliente lo llama al recargar la pagina para recuperar la sesion.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
            return Problem(title: SessionExpiredMessage, statusCode: StatusCodes.Status401Unauthorized);

        var result = await _authService.RefreshAsync(refreshToken, cancellationToken);

        if (!result.Succeeded)
        {
            DeleteRefreshCookie();
            return Problem(title: SessionExpiredMessage, statusCode: StatusCodes.Status401Unauthorized);
        }

        SetRefreshCookie(result.Response!);
        return Ok(result.Response);
    }

    /// <summary>Revoca el refresh token y borra la cookie.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
            await _authService.LogoutAsync(refreshToken, cancellationToken);

        DeleteRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(AuthResponse response)
    {
        Response.Cookies.Append(RefreshCookieName, response.RefreshToken, new CookieOptions
        {
            HttpOnly = true,                  // JavaScript no puede leerla
            Secure = _secureCookie,           // solo por HTTPS (en produccion)
            SameSite = SameSiteMode.Strict,   // no se envia desde otros sitios (CSRF)
            Path = RefreshCookiePath,
            Expires = response.RefreshTokenExpiresAtUtc
        });
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = _secureCookie,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath
        });
    }
}
