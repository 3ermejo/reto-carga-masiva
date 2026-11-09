using Auth.Api.Application.Dtos;

namespace Auth.Api.Application.Results;

public sealed class AuthResult
{
    private AuthResult()
    {
    }

    public bool Succeeded { get; private init; }
    public AuthResponse? Response { get; private init; }

    public static AuthResult Success(AuthResponse response) => new() { Succeeded = true, Response = response };

    // A proposito no se indica el motivo (usuario inexistente, clave incorrecta, bloqueado)
    public static AuthResult Failure() => new() { Succeeded = false };
}
