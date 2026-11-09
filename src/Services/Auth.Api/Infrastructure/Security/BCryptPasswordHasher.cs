using Auth.Api.Application.Interfaces;

namespace Auth.Api.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    // Hash ficticio para comparar cuando el usuario no existe
    private readonly string _dummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor);

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string? passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            BCrypt.Net.BCrypt.Verify(password, _dummyHash);
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
