using Auth.Api.Application.Interfaces;
using Dapper;
using Npgsql;

namespace Auth.Api.Infrastructure.Persistence;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public RefreshTokenRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task SaveAsync(int userId, string tokenHash, int validDays, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL seguridad.sp_guardar_refresh_token(@UserId, @TokenHash, @ValidDays)",
            new { UserId = userId, TokenHash = tokenHash, ValidDays = validDays },
            cancellationToken: cancellationToken));
    }

    public async Task<int?> RotateAsync(string tokenHash, string newTokenHash, int validDays, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT seguridad.fn_rotar_refresh_token(@TokenHash, @NewTokenHash, @ValidDays)",
            new { TokenHash = tokenHash, NewTokenHash = newTokenHash, ValidDays = validDays },
            cancellationToken: cancellationToken));
    }

    public async Task RevokeAsync(string tokenHash, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "CALL seguridad.sp_revocar_refresh_token(@TokenHash)",
            new { TokenHash = tokenHash },
            cancellationToken: cancellationToken));
    }
}
