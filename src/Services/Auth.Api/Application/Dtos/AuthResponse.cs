using System.Text.Json.Serialization;

namespace Auth.Api.Application.Dtos;

public record UsuarioResponse(int Id, string Email, string NombreCompleto, string Rol);

/// <summary>
/// El refresh token NO viaja en el body: el controller lo envia en una cookie HttpOnly
/// para que JavaScript no pueda leerlo (protege ante XSS).
/// </summary>
public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UsuarioResponse Usuario,
    [property: JsonIgnore] string RefreshToken,
    [property: JsonIgnore] DateTime RefreshTokenExpiresAtUtc);
