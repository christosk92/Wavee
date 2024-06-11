using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify;
using Wavee.UI.Spotify.Common;
using Wavee.UI.Spotify.Interfaces.Api;

namespace Wavee.UI.Spotify.Interfaces;

/// <summary>
/// Represents an interface for handling Spotify authentication.
/// </summary>
public interface ISpotifyAuthModule
{
    ISpotifyAccountClient AccountClient { get; }
    ISpotifyLoginClient LoginClient { get; }

    /// <summary>
    /// Logs in and returns a Spotify connection.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the login operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an instance of <see cref="ISpotifyConnection"/> if the login is successful; otherwise, it throws an exception.
    /// </returns>
    ValueTask<ISpotifyConnection> Login(CancellationToken cancellationToken);
}

public interface ISpotifyConnection : IDisposable
{
    bool IsConnected { get; }
    LoginCredentials AuthenticatedCredentials { get; }
    Task<byte[]> GetAudioKey(RegularSpotifyId itemId, string fileId, CancellationToken cancellationToken);
}