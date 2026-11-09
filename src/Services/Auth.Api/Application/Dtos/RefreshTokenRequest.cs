using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Application.Dtos;

public record RefreshTokenRequest
{
    [Required(ErrorMessage = "El refresh token es obligatorio.")]
    [StringLength(200)]
    public string RefreshToken { get; init; } = string.Empty;
}
