using System.Text.RegularExpressions;

namespace Database.Migrator;

public record SeedUser(string Email, string FullName, string Role, string Password);

public record MigratorSettings(
    string ConnectionString,
    string DatabaseName,
    IReadOnlyDictionary<string, string> ServiceRolePasswords,
    IReadOnlyList<SeedUser> SeedUsers)
{
    // Las claves van dentro de un ALTER ROLE, por eso solo se aceptan letras y numeros
    private static readonly Regex SafePassword = new("^[A-Za-z0-9]{12,128}$", RegexOptions.None, TimeSpan.FromSeconds(1));

    public static MigratorSettings FromEnvironment()
    {
        var connectionString = Required("ConnectionStrings__Admin");
        var databaseName = Required("POSTGRES_DB");

        var rolePasswords = new Dictionary<string, string>
        {
            ["auth_svc"] = ServicePassword("AUTH_DB_PASSWORD"),
            ["control_svc"] = ServicePassword("CONTROL_DB_PASSWORD"),
            ["bulkload_svc"] = ServicePassword("BULKLOAD_DB_PASSWORD"),
            ["notification_svc"] = ServicePassword("NOTIFICATION_DB_PASSWORD")
        };

        var seedUsers = new List<SeedUser>
        {
            new("admin@cargamasiva.local", "Administrador del Sistema", "Administrador", Required("SEED_ADMIN_PASSWORD")),
            new("operador@cargamasiva.local", "Operador de Cargas", "Operador", Required("SEED_OPERADOR_PASSWORD")),
            new("consulta@cargamasiva.local", "Usuario de Consulta", "Consulta", Required("SEED_CONSULTA_PASSWORD"))
        };

        return new MigratorSettings(connectionString, databaseName, rolePasswords, seedUsers);
    }

    private static string ServicePassword(string name)
    {
        var value = Required(name);
        if (!SafePassword.IsMatch(value))
            throw new InvalidOperationException($"{name} debe tener entre 12 y 128 letras o números.");

        return value;
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Falta la variable de entorno {name}.");

        return value;
    }
}
