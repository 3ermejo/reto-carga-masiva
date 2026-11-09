using Npgsql;

namespace Database.Migrator;

/// <summary>
/// Cada microservicio se conecta con su propio usuario de PostgreSQL.
/// Se ejecuta en cada arranque para que las contraseñas siempre coincidan con el .env.
/// </summary>
public static class ServiceRolesSetup
{
    public static async Task ApplyAsync(MigratorSettings settings)
    {
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync();

        foreach (var (role, password) in settings.ServiceRolePasswords)
        {
            var sql = $"""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{role}') THEN
                        CREATE ROLE {role} LOGIN;
                    END IF;
                END
                $$;
                ALTER ROLE {role} WITH LOGIN PASSWORD '{password}';
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
            Console.WriteLine($"Usuario de base de datos '{role}' listo.");
        }

        // Nadie mas que estos usuarios puede conectarse a la base
        var roles = string.Join(", ", settings.ServiceRolePasswords.Keys);
        var grants = $"""
            REVOKE ALL ON DATABASE "{settings.DatabaseName}" FROM PUBLIC;
            GRANT CONNECT ON DATABASE "{settings.DatabaseName}" TO {roles};
            """;

        await using var grantCommand = new NpgsqlCommand(grants, connection);
        await grantCommand.ExecuteNonQueryAsync();
    }
}
