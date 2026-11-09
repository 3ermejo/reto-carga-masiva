namespace Control.Api.Domain;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
    public const string Consulta = "Consulta";

    /// <summary>Roles que pueden ver las cargas de todos los usuarios.</summary>
    public static bool PuedeVerTodas(string rol) => rol is Administrador or Consulta;
}
