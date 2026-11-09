using System.Reflection;
using Npgsql;

namespace Database.Migrator;

/// <summary>
/// Migraciones automaticas estilo Flyway: cada script V###__descripcion.sql se ejecuta
/// una sola vez, en orden y dentro de una transaccion. Los aplicados quedan registrados
/// en public.historial_migraciones.
/// </summary>
public static class ScriptMigrator
{
    private const string CreateHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS public.historial_migraciones (
            version     varchar(150) PRIMARY KEY,
            aplicado_en timestamptz  NOT NULL DEFAULT now()
        );
        """;

    public static async Task ApplyAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using (var command = new NpgsqlCommand(CreateHistoryTableSql, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        var applied = await GetAppliedVersionsAsync(connection);
        var assembly = Assembly.GetExecutingAssembly();

        var scripts = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(name => (Resource: name, Version: ToVersion(name)))
            .OrderBy(script => script.Version, StringComparer.Ordinal)
            .ToList();

        foreach (var script in scripts)
        {
            if (applied.Contains(script.Version))
                continue;

            var sql = await ReadResourceAsync(assembly, script.Resource);

            await using var transaction = await connection.BeginTransactionAsync();

            await using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                await command.ExecuteNonQueryAsync();
            }

            await using (var command = new NpgsqlCommand(
                "INSERT INTO public.historial_migraciones (version) VALUES (@version)", connection, transaction))
            {
                command.Parameters.AddWithValue("version", script.Version);
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            Console.WriteLine($"Script aplicado: {script.Version}");
        }
    }

    private static async Task<HashSet<string>> GetAppliedVersionsAsync(NpgsqlConnection connection)
    {
        var versions = new HashSet<string>(StringComparer.Ordinal);

        await using var command = new NpgsqlCommand("SELECT version FROM public.historial_migraciones", connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            versions.Add(reader.GetString(0));

        return versions;
    }

    // "Database.Migrator.Scripts.V001__esquemas.sql" -> "V001__esquemas.sql"
    private static string ToVersion(string resourceName)
    {
        var start = resourceName.IndexOf(".V", StringComparison.Ordinal);
        return start >= 0 ? resourceName[(start + 1)..] : resourceName;
    }

    private static async Task<string> ReadResourceAsync(Assembly assembly, string resourceName)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"No se encontró el script {resourceName}.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
