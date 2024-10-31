using Eum.Spotify;

namespace Wavee.Config;

/// <summary>
/// Defines a delegate for retrieving Spotify login credentials asynchronously.
/// </summary>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains the retrieved <see cref="LoginCredentials"/> or <c>null</c> if no credentials are found.
/// </returns>
public delegate ValueTask<LoginCredentials?> RetrieveCredentialsDelegate(CancellationToken cancellationToken);