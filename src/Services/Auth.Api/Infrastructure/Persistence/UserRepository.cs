using Auth.Api.Application.Interfaces;
using Auth.Api.Domain;
using Dapper;
using Npgsql;

namespace Auth.Api.Infrastructure.Persistence;

/// <summary>Acceso a datos con Dapper llamando a las funciones/procedimientos del esquema seguridad.</summary>
public class UserRepository : IUserRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UserRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Usuario>(new CommandDefinition(
            "SELECT * FROM seguridad.fn_obtener_usuario_por_email(@Email)",
            new { Email = email },
            cancellationToken: cancellationToken));
    }

    public async Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Usuario>(new CommandDefinition(
            "SELECT * FROM seguridad.fn_obtener_usuario_por_id(@Id)",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task RegisterFailedLoginAsync(int userId, int maxAttempts, int lockoutMinutes, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL seguridad.sp_registrar_login_fallido(@UserId, @MaxAttempts, @LockoutMinutes)",
            new { UserId = userId, MaxAttempts = maxAttempts, LockoutMinutes = lockoutMinutes },
            cancellationToken: cancellationToken));
    }

    public async Task RegisterSuccessfulLoginAsync(int userId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL seguridad.sp_registrar_login_exitoso(@UserId)",
            new { UserId = userId },
            cancellationToken: cancellationToken));
    }
}
