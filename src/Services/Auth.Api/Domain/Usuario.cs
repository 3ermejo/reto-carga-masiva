namespace Auth.Api.Domain;

/// <summary>Usuario tal como lo devuelve seguridad.fn_obtener_usuario_por_email.</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public bool Bloqueado { get; set; }

    public bool PuedeIniciarSesion => Activo && !Bloqueado;
}
