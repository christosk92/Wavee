using System.Text.Json;
using Eum.Spotify;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Application.Authentication.Requests;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Exceptions;
using Wavee.Spotify.Infrastructure.LegacyAuth;
using Wavee.Spotify.Infrastructure.Persistent;

namespace Wavee.Spotify.Application.Authentication.Modules;

/// <summary>
/// A module that uses stored credentials to authenticate with Spotify.
///
/// If no stored credentials are found, this module will throw a <see cref="SpotifyNoStoredCredentialsException"/>.
/// </summary>
public sealed class SpotifyStoredCredentialsModule : ISpotifyAuthModule
{
    private readonly ISpotifyStoredCredentialsRepository _spotifyStoredCredentialsRepository;
    private readonly IMediator _mediator;
    private readonly SpotifyTcpHolder _tcpHolder;
    private readonly SpotifyClientConfig _config;

    public SpotifyStoredCredentialsModule(
        IMediator mediator,
        ISpotifyStoredCredentialsRepository spotifyStoredCredentialsRepository,
        SpotifyTcpHolder tcpHolder, SpotifyClientConfig config)
    {
        _mediator = mediator;
        _spotifyStoredCredentialsRepository = spotifyStoredCredentialsRepository;
        _tcpHolder = tcpHolder;
        _config = config;
    }

    public bool IsDefault { get; private set; }

    public async Task<StoredCredentials> GetCredentials(string? username, CancellationToken cancellationToken = default)
    {
        var storedCredentials =
            await _spotifyStoredCredentialsRepository.GetStoredCredentials(username, cancellationToken);
        if (storedCredentials is null)
        {
            //No stored credential, throw exception
            throw new SpotifyNoStoredCredentialsException();
        }

        IsDefault = storedCredentials.IsDefault;

        await _tcpHolder.Connect(
            credentials: new LoginCredentials
            {
                AuthData = ByteString.FromBase64(storedCredentials.ReusableCredentialsBase64),
                Username = storedCredentials.Username,
                Typ = (AuthenticationType)storedCredentials.ReusableCredentialsType
            },
            deviceId: _config.Remote.DeviceId,
            cancellationToken: cancellationToken
        );

        return storedCredentials;
    }
}

public record StoredCredentials(string Username, string ReusableCredentialsBase64, int ReusableCredentialsType,
    bool IsDefault);