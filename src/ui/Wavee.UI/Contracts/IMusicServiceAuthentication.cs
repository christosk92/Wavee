using Google.Protobuf;
using Wavee.UI.User;

namespace Wavee.UI.Contracts;

/// <summary>
/// Represents a contract for authenticating with a music service.
/// </summary>
public interface IMusicServiceAuthentication
{
    /// <summary>
    /// Performs authentication with the music service.
    /// </summary>
    /// <param name="username">
    /// The username to use for authentication.
    /// </param>
    /// <param name="password">
    /// The password to use for authentication.
    /// </param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="UserViewModel"/> if authentication was successful; otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="MusicAuthenticationException"></exception>
    Task<UserViewModel?> Authenticate(string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Performs authentication with the music service using reusable credentials.
    /// </summary>
    /// <param name="username">
    /// The canonical username to use for authentication.
    /// </param>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="UserViewModel"/> if authentication was successful; otherwise, <see langword="null"/>.
    /// </returns>
    Task<UserViewModel?> AuthenticateStored(string username, CancellationToken ct = default);
}

public sealed class MusicAuthenticationException : Exception
{
    public MusicAuthenticationException(string message) : base(message)
    {
    }
}