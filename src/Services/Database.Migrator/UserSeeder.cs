using Npgsql;

namespace Database.Migrator;

/// <summary>
/// Usuarios de prueba (uno por rol). Las contraseñas vienen del .env y se guardan con BCrypt.
/// Si el usuario ya existe no se modifica.
/// </summary>
public static class UserSeeder
{
    private const int WorkFactor = 12;

    public static async Task SeedAsync(MigratorSettings settings)
    {
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync();

        foreach (var user in settings.SeedUsers)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO seguridad.usuario (email, nombre_completo, password_hash, rol)
                VALUES (@email, @nombre, @hash, @rol)
                ON CONFLICT (email) DO NOTHING
                """, connection);

            command.Parameters.AddWithValue("email", user.Email);
            command.Parameters.AddWithValue("nombre", user.FullName);
            command.Parameters.AddWithValue("hash", BCrypt.Net.BCrypt.HashPassword(user.Password, WorkFactor));
            command.Parameters.AddWithValue("rol", user.Role);

            var inserted = await command.ExecuteNonQueryAsync();
            if (inserted > 0)
                Console.WriteLine($"Usuario de prueba creado: {user.Email} ({user.Role})");
        }
    }
}
