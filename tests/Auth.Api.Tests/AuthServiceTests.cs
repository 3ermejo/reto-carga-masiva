using Auth.Api.Application.Dtos;
using Auth.Api.Application.Interfaces;
using Auth.Api.Application.Services;
using Auth.Api.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Auth.Api.Tests;

public class AuthServiceTests
{
    private const string Password = "Clave$egura123";

    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _users.Add(new Usuario { Id = 1, Email = "admin@test.com", NombreCompleto = "Admin", PasswordHash = $"hash:{Password}", Rol = Roles.Administrador, Activo = true });
        _users.Add(new Usuario { Id = 2, Email = "bloqueado@test.com", NombreCompleto = "Bloqueado", PasswordHash = $"hash:{Password}", Rol = Roles.Operador, Activo = true, Bloqueado = true });

        _service = new AuthService(
            _users,
            _refreshTokens,
            new FakePasswordHasher(),
            new FakeTokenService(),
            Options.Create(new LockoutOptions { MaxIntentosFallidos = 5, MinutosBloqueo = 15 }),
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Login_ConCredencialesCorrectas_DevuelveTokensYGuardaElRefreshToken()
    {
        var result = await _service.LoginAsync(new LoginRequest { Email = " ADMIN@test.com ", Password = Password }, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("access-1", result.Response!.AccessToken);
        Assert.Equal(Roles.Administrador, result.Response.Usuario.Rol);
        Assert.Single(_refreshTokens.Saved);
        Assert.Equal(1, _users.SuccessfulLogins[0]);
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_RegistraElIntentoFallido()
    {
        var result = await _service.LoginAsync(new LoginRequest { Email = "admin@test.com", Password = "Otra$Clave1" }, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(1, _users.FailedLogins[0]);
    }

    [Fact]
    public async Task Login_ConUsuarioInexistente_Falla()
    {
        var result = await _service.LoginAsync(new LoginRequest { Email = "nadie@test.com", Password = Password }, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(_users.FailedLogins);
    }

    [Fact]
    public async Task Login_ConCuentaBloqueada_FallaAunqueLaClaveSeaCorrecta()
    {
        var result = await _service.LoginAsync(new LoginRequest { Email = "bloqueado@test.com", Password = Password }, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(_refreshTokens.Saved);
    }

    [Fact]
    public async Task Refresh_ConTokenValido_DevuelveNuevosTokens()
    {
        _refreshTokens.ValidHashes["hash-refresh-viejo"] = 1;

        var result = await _service.RefreshAsync("refresh-viejo", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotEqual("refresh-viejo", result.Response!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ConTokenInvalido_Falla()
    {
        var result = await _service.RefreshAsync("no-existe", CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<Usuario> _users = [];
        public List<int> FailedLogins { get; } = [];
        public List<int> SuccessfulLogins { get; } = [];

        public void Add(Usuario usuario) => _users.Add(usuario);

        public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

        public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task RegisterFailedLoginAsync(int userId, int maxAttempts, int lockoutMinutes, CancellationToken cancellationToken)
        {
            FailedLogins.Add(userId);
            return Task.CompletedTask;
        }

        public Task RegisterSuccessfulLoginAsync(int userId, CancellationToken cancellationToken)
        {
            SuccessfulLogins.Add(userId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<string> Saved { get; } = [];
        public Dictionary<string, int> ValidHashes { get; } = [];

        public Task SaveAsync(int userId, string tokenHash, int validDays, CancellationToken cancellationToken)
        {
            Saved.Add(tokenHash);
            return Task.CompletedTask;
        }

        public Task<int?> RotateAsync(string tokenHash, string newTokenHash, int validDays, CancellationToken cancellationToken)
        {
            return Task.FromResult(ValidHashes.TryGetValue(tokenHash, out var userId) ? userId : (int?)null);
        }

        public Task RevokeAsync(string tokenHash, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // Hasher simple para que las pruebas no dependan de BCrypt (que es lento a proposito)
    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string? passwordHash) => passwordHash == $"hash:{password}";
    }

    private sealed class FakeTokenService : ITokenService
    {
        private int _refreshCounter;

        public int RefreshTokenDays => 7;
        public AccessToken CreateAccessToken(Usuario usuario) => new("access-1", DateTime.UtcNow.AddMinutes(15));
        public string CreateRefreshToken() => $"refresh-{++_refreshCounter}";
        public string HashRefreshToken(string refreshToken) => $"hash-{refreshToken}";
    }
}
