using Auth.Api.Application.Dtos;
using Auth.Api.Application.Interfaces;
using Auth.Api.Application.Results;
using Auth.Api.Domain;
using Microsoft.Extensions.Options;

namespace Auth.Api.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly LockoutOptions _lockout;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<LockoutOptions> lockout,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _lockout = lockout.Value;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (usuario is null)
        {
            // Se verifica igual para no dar pistas por tiempo de respuesta
            _passwordHasher.Verify(request.Password, null);
            _logger.LogWarning("Intento de login con un correo no registrado");
            return AuthResult.Failure();
        }

        if (!usuario.PuedeIniciarSesion)
        {
            _logger.LogWarning("Intento de login sobre la cuenta bloqueada o inactiva {UserId}", usuario.Id);
            return AuthResult.Failure();
        }

        if (!_passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            await _userRepository.RegisterFailedLoginAsync(usuario.Id, _lockout.MaxIntentosFallidos, _lockout.MinutosBloqueo, cancellationToken);
            _logger.LogWarning("Contraseña incorrecta para el usuario {UserId}", usuario.Id);
            return AuthResult.Failure();
        }

        await _userRepository.RegisterSuccessfulLoginAsync(usuario.Id, cancellationToken);
        _logger.LogInformation("Inicio de sesión del usuario {UserId}", usuario.Id);

        return AuthResult.Success(await IssueTokensAsync(usuario, cancellationToken));
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var newRefreshToken = _tokenService.CreateRefreshToken();

        var userId = await _refreshTokenRepository.RotateAsync(
            _tokenService.HashRefreshToken(refreshToken),
            _tokenService.HashRefreshToken(newRefreshToken),
            _tokenService.RefreshTokenDays,
            cancellationToken);

        if (userId is null)
        {
            _logger.LogWarning("Refresh token inválido, vencido o reutilizado");
            return AuthResult.Failure();
        }

        var usuario = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (usuario is null || !usuario.PuedeIniciarSesion)
            return AuthResult.Failure();

        var accessToken = _tokenService.CreateAccessToken(usuario);
        return AuthResult.Success(BuildResponse(usuario, accessToken, newRefreshToken));
    }

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return _refreshTokenRepository.RevokeAsync(_tokenService.HashRefreshToken(refreshToken), cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.CreateAccessToken(usuario);
        var refreshToken = _tokenService.CreateRefreshToken();

        // En la BD solo se guarda el hash del refresh token
        await _refreshTokenRepository.SaveAsync(
            usuario.Id, _tokenService.HashRefreshToken(refreshToken), _tokenService.RefreshTokenDays, cancellationToken);

        return BuildResponse(usuario, accessToken, refreshToken);
    }

    private AuthResponse BuildResponse(Usuario usuario, AccessToken accessToken, string refreshToken)
    {
        return new AuthResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            new UsuarioResponse(usuario.Id, usuario.Email, usuario.NombreCompleto, usuario.Rol),
            refreshToken,
            DateTime.UtcNow.AddDays(_tokenService.RefreshTokenDays));
    }
}
