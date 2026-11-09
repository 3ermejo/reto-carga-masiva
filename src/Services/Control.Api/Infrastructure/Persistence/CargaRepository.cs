using Control.Api.Application.Interfaces;
using Control.Api.Domain;
using Dapper;
using Npgsql;

namespace Control.Api.Infrastructure.Persistence;

/// <summary>Acceso a datos con Dapper: solo se llaman funciones y procedimientos del esquema carga.</summary>
public class CargaRepository : ICargaRepository
{
    private const string Origen = "control-api";

    private readonly NpgsqlDataSource _dataSource;

    public CargaRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<int> RegistrarAsync(string nombreArchivo, string usuario, int usuarioId, string rutaArchivo, long tamanoBytes, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT carga.fn_registrar_carga(@NombreArchivo, @Usuario, @UsuarioId, @RutaArchivo, @TamanoBytes)",
            new { NombreArchivo = nombreArchivo, Usuario = usuario, UsuarioId = usuarioId, RutaArchivo = rutaArchivo, TamanoBytes = tamanoBytes },
            cancellationToken: cancellationToken));
    }

    public async Task FinalizarConErrorAsync(int idCarga, string mensaje, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL carga.sp_finalizar_carga(@IdCarga, 0, 0, 0, 'Error', @Mensaje, @Origen)",
            new { IdCarga = idCarga, Mensaje = mensaje, Origen },
            cancellationToken: cancellationToken));
    }

    public async Task<Carga?> ObtenerAsync(int idCarga, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Carga>(new CommandDefinition(
            "SELECT * FROM carga.fn_obtener_carga(@IdCarga)",
            new { IdCarga = idCarga },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<CargaResumen>> ListarAsync(int? usuarioId, int limite, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var cargas = await connection.QueryAsync<CargaResumen>(new CommandDefinition(
            "SELECT * FROM carga.fn_listar_cargas(@UsuarioId, @Limite)",
            new { UsuarioId = usuarioId, Limite = limite },
            cancellationToken: cancellationToken));
        return cargas.AsList();
    }

    public async Task<IReadOnlyList<CargaHistorial>> ListarHistorialAsync(int idCarga, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var historial = await connection.QueryAsync<CargaHistorial>(new CommandDefinition(
            "SELECT * FROM carga.fn_listar_historial(@IdCarga)",
            new { IdCarga = idCarga },
            cancellationToken: cancellationToken));
        return historial.AsList();
    }

    public async Task<IReadOnlyList<RegistroProcesado>> ListarRegistrosAsync(int idCarga, int pagina, int tamano, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var registros = await connection.QueryAsync<RegistroProcesado>(new CommandDefinition(
            "SELECT * FROM carga.fn_listar_registros(@IdCarga, @Pagina, @Tamano)",
            new { IdCarga = idCarga, Pagina = pagina, Tamano = tamano },
            cancellationToken: cancellationToken));
        return registros.AsList();
    }

    public async Task<IReadOnlyList<RegistroFallido>> ListarFallidosAsync(int idCarga, int pagina, int tamano, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var fallidos = await connection.QueryAsync<RegistroFallido>(new CommandDefinition(
            "SELECT * FROM carga.fn_listar_fallidos(@IdCarga, @Pagina, @Tamano)",
            new { IdCarga = idCarga, Pagina = pagina, Tamano = tamano },
            cancellationToken: cancellationToken));
        return fallidos.AsList();
    }
}
