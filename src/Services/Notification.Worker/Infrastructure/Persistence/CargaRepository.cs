using Dapper;
using Notification.Worker.Application.Interfaces;
using Notification.Worker.Domain;
using Npgsql;

namespace Notification.Worker.Infrastructure.Persistence;

public class CargaRepository : ICargaRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CargaRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<CargaNotificacion?> ObtenerAsync(int idCarga, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CargaNotificacion>(new CommandDefinition(
            """
            SELECT id, nombre_archivo, usuario, periodos, estado, resultado, mensaje,
                   total_registros, registros_insertados, registros_fallidos, fecha_registro, fecha_fin
            FROM carga.fn_obtener_carga(@IdCarga)
            """,
            new { IdCarga = idCarga },
            cancellationToken: cancellationToken));
    }

    public async Task MarcarNotificadoAsync(int idCarga, string destinatario, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL carga.sp_marcar_notificado(@IdCarga, @Destinatario)",
            new { IdCarga = idCarga, Destinatario = destinatario },
            cancellationToken: cancellationToken));
    }
}
