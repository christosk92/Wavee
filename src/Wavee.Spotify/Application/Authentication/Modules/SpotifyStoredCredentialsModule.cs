using System.Text.Json;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Application.Authentication.Requests;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Exceptions;

namespace Wavee.Spotify.Application.Authentication.Modules;

/// <summary>
/// A module that uses stored credentials to authenticate with Spotify.
///
/// If no stored credentials are found, this module will throw a <see cref="SpotifyNoStoredCredentialsException"/>.
/// </summary>
public sealed class SpotifyStoredCredentialsModule : ISpotifyAuthModule
{
    private readonly SpotifyClientConfig _config;
    private readonly IMediator _mediator;

    public SpotifyStoredCredentialsModule(
        SpotifyClientConfig config,
        IMediator mediator)
    {
        _config = config;
        _mediator = mediator;
    }

    public async Task<StoredCredentials> GetCredentials(CancellationToken cancellationToken = default)
    {
        var storedCredentials = await GetStoredCredentials();
        if (storedCredentials is null)
        {
            //No stored credential, throw exception
            throw new SpotifyNoStoredCredentialsException();
        }

        return storedCredentials;
    }

    private async Task<StoredCredentials?> GetStoredCredentials()
    {
        try
        {
            var storedCredentials = Path.Combine(_config.Storage.Path, "credentials.json");
            if (File.Exists(storedCredentials))
            {
                var json = await File.ReadAllTextAsync(storedCredentials);
                var result = JsonSerializer.Deserialize<StoredCredentials>(json);
                return result;
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

public record StoredCredentials(string Username, string ReusableCredentialsBase64, int ReusableCredentialsType);