using System.Text.Json;
using BulkLoad.Worker.Application.Interfaces;
using BulkLoad.Worker.Domain;
using Dapper;
using Npgsql;

namespace BulkLoad.Worker.Infrastructure.Persistence;

/// <summary>
/// Acceso a datos con Dapper: solo se llaman funciones y procedimientos del esquema carga.
/// Los registros se envian como JSON en una sola llamada (jsonb_to_recordset en la BD).
/// </summary>
public class CargaRepository : ICargaRepository
{
    private const string Origen = "bulkload-worker";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly NpgsqlDataSource _dataSource;

    public CargaRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<string> IniciarProcesoAsync(int idCarga, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var resultado = await connection.ExecuteScalarAsync<string>(new CommandDefinition(
            "SELECT carga.fn_iniciar_proceso(@IdCarga)",
            new { IdCarga = idCarga },
            cancellationToken: cancellationToken));

        return resultado ?? InicioProceso.NoExiste;
    }

    public async Task<ValidacionPeriodo> ValidarPeriodosAsync(int idCarga, IReadOnlyCollection<string> periodos, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<ValidacionPeriodo>(new CommandDefinition(
            "SELECT * FROM carga.fn_validar_periodos(@IdCarga, CAST(@Periodos AS varchar[]))",
            new { IdCarga = idCarga, Periodos = periodos.ToArray() },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ResultadoInsercion>> InsertarRegistrosAsync(int idCarga, IReadOnlyCollection<RegistroProducto> registros, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var resultados = await connection.QueryAsync<ResultadoInsercion>(new CommandDefinition(
            "SELECT fila, codigo_producto, insertado FROM carga.fn_insertar_registros(@IdCarga, CAST(@Registros AS jsonb))",
            new { IdCarga = idCarga, Registros = JsonSerializer.Serialize(registros, JsonOptions) },
            commandTimeout: 300,
            cancellationToken: cancellationToken));

        return resultados.AsList();
    }

    public async Task RegistrarFallidosAsync(int idCarga, IReadOnlyCollection<RegistroFallido> fallidos, CancellationToken cancellationToken)
    {
        if (fallidos.Count == 0)
            return;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL carga.sp_registrar_fallidos(@IdCarga, CAST(@Fallidos AS jsonb))",
            new { IdCarga = idCarga, Fallidos = JsonSerializer.Serialize(fallidos, JsonOptions) },
            commandTimeout: 300,
            cancellationToken: cancellationToken));
    }

    public async Task MarcarCargadoAsync(int idCarga, int registrosInsertados, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL carga.sp_marcar_cargado(@IdCarga, @Insertados)",
            new { IdCarga = idCarga, Insertados = registrosInsertados },
            cancellationToken: cancellationToken));
    }

    public async Task FinalizarAsync(int idCarga, int total, int insertados, int fallidos, string resultado, string? mensaje, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL carga.sp_finalizar_carga(@IdCarga, @Total, @Insertados, @Fallidos, @Resultado, @Mensaje, @Origen)",
            new { IdCarga = idCarga, Total = total, Insertados = insertados, Fallidos = fallidos, Resultado = resultado, Mensaje = mensaje, Origen },
            cancellationToken: cancellationToken));
    }
}
