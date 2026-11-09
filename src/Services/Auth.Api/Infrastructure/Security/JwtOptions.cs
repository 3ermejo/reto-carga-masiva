using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    // HS256 necesita una clave de al menos 256 bits (32 bytes)
    [Required]
    [MinLength(32)]
    public string Key { get; init; } = string.Empty;

    [Range(1, 120)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 30)]
    public int RefreshTokenDays { get; init; } = 7;

    /// <summary>
    /// La cookie del refresh token solo viaja por HTTPS cuando es true.
    /// En local (http://localhost) docker-compose lo pone en false.
    /// </summary>
    public bool RefreshCookieSecure { get; init; } = true;
}
