using Eum.Spotify;

namespace Wavee.Config;

/// <summary>
/// Defines a delegate for storing Spotify login credentials asynchronously.
/// </summary>
/// <param name="credentials">The <see cref="LoginCredentials"/> to be stored.</param>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
/// <returns>
/// A task that represents the asynchronous operation.
/// </returns>
public delegate ValueTask StoreCredentialsDelegate(LoginCredentials credentials, CancellationToken cancellationToken);