using Npgsql;

namespace Database.Migrator;

public static class DatabaseWaiter
{
    public static async Task WaitUntilAvailableAsync(string connectionString, int maxAttempts = 30)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                return;
            }
            catch (NpgsqlException) when (attempt < maxAttempts)
            {
                Console.WriteLine($"Esperando a PostgreSQL (intento {attempt}/{maxAttempts})...");
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
