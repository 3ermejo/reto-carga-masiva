namespace Auth.Api.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Si el hash es null igual se ejecuta una verificacion (contra un hash ficticio)
    /// para que el tiempo de respuesta no revele si el usuario existe.
    /// </summary>
    bool Verify(string password, string? passwordHash);
}
