using Auth.Api.Domain;

namespace Auth.Api.Application.Interfaces;

public interface IUserRepository
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task RegisterFailedLoginAsync(int userId, int maxAttempts, int lockoutMinutes, CancellationToken cancellationToken);
    Task RegisterSuccessfulLoginAsync(int userId, CancellationToken cancellationToken);
}
