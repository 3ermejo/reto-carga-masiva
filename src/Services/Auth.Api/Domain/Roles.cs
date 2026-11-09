namespace Auth.Api.Domain;

public static class Roles
{
    // Puede subir archivos y ver todas las cargas
    public const string Administrador = "Administrador";

    // Puede subir archivos y ver sus propias cargas
    public const string Operador = "Operador";

    // Solo puede consultar (no tiene permiso para ejecutar cargas masivas)
    public const string Consulta = "Consulta";
}
