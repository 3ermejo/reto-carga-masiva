using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Application.Dtos;

public record LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(128)]
    public string Password { get; init; } = string.Empty;
}
