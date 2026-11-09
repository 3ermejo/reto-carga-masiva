using System.Security.Claims;
using Control.Api.Application.Common;

namespace Control.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;
        var name = principal.FindFirst("name")?.Value;
        var role = principal.FindFirst("role")?.Value;

        if (!int.TryParse(id, out var userId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
            throw new UnauthorizedAccessException("El token no contiene la identidad del usuario.");

        return new CurrentUser(userId, email, string.IsNullOrWhiteSpace(name) ? email : name, role);
    }
}
