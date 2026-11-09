using Auth.Api.Domain;

namespace Auth.Api.Application.Interfaces;

public record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken CreateAccessToken(Usuario usuario);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    int RefreshTokenDays { get; }
}
