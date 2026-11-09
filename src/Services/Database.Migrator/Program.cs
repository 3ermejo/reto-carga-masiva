using Database.Migrator;

// Migrador de base de datos: se ejecuta una vez al levantar docker-compose
// y los microservicios esperan a que termine bien antes de iniciar.
//   1. Crea/actualiza los usuarios de BD de cada microservicio
//   2. Aplica los scripts versionados pendientes (Scripts/V###__*.sql)
//   3. Crea los usuarios de prueba de la aplicacion si no existen

try
{
    var settings = MigratorSettings.FromEnvironment();

    await DatabaseWaiter.WaitUntilAvailableAsync(settings.ConnectionString);
    await ServiceRolesSetup.ApplyAsync(settings);
    await ScriptMigrator.ApplyAsync(settings.ConnectionString);
    await UserSeeder.SeedAsync(settings);

    Console.WriteLine("Migración completada.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"La migración falló: {ex.Message}");
    return 1;
}
