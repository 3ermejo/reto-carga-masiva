namespace Auth.Api.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task SaveAsync(int userId, string tokenHash, int validDays, CancellationToken cancellationToken);

    /// <summary>Revoca el token usado y guarda el nuevo. Devuelve el id del usuario o null si el token no es valido.</summary>
    Task<int?> RotateAsync(string tokenHash, string newTokenHash, int validDays, CancellationToken cancellationToken);

    Task RevokeAsync(string tokenHash, CancellationToken cancellationToken);
}
