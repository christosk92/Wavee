using System.Globalization;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Id;
using Wavee.Infrastructure.Authentication;
using Wavee.UI.Client;
using Wavee.UI.Contracts;
using Wavee.UI.User;

namespace Wavee.UI.Spotify.Authentication;

internal sealed class SpotifyAuthentication : IMusicServiceAuthentication
{
    private readonly IUserManager _userManager;

    public SpotifyAuthentication(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserViewModel?> Authenticate(string username, string password, CancellationToken ct = default)
    {
        try
        {
            var credentials = new LoginCredentials
            {
                Username = username,
                AuthData = ByteString.CopyFromUtf8(password),
                Typ = AuthenticationType.AuthenticationUserPass
            };
            var cachePath = Path.Combine(
                AppProviders.GetPersistentStoragePath(),
                "Wavee",
                "Spotify"
            );
            Directory.CreateDirectory(cachePath);
            var config = new SpotifyConfig(
                Remote: new SpotifyRemoteConfig(
                    deviceName: "Wavee",
                    deviceType: DeviceType.Computer
                ),
                Cache: new SpotifyCacheConfig(
                    CacheLocation: cachePath,
                    MaxCacheSize: Option<long>.None
                ),
                playback: new SpotifyPlaybackConfig(
                    preferedQuality: PreferedQuality.Normal,
                    crossfadeDuration: Option<TimeSpan>.None),
                locale: CultureInfo.CurrentUICulture
            );
            var spotifyClient = new SpotifyClient(Shared.Player, credentials, config);
            var user = await spotifyClient.Metadata.GetMe(ct);
            var welcomeMessage = spotifyClient.WelcomeMessage;
            var newUser = new UserViewModel(
                id: new UserId(Source: ServiceType.Spotify, Id: welcomeMessage.CanonicalUsername),
                displayName: user.DisplayName,
                image:user.Images.HeadOrNone().Map(x => x.Url).IfNone(string.Empty),
                client: new WaveeUIClient(spotifyClient))
            {
                ReusableCredentials = welcomeMessage.ReusableAuthCredentials.ToBase64()
            };
            _userManager.SaveUser(newUser, true);
            return newUser;
        }
        catch (SpotifyAuthenticationException authenticationException)
        {
            throw new MusicAuthenticationException(
                authenticationException.ErrorCode.ErrorCode switch
                {
                    ErrorCode.BadCredentials => "Invalid username or password",
                    _ => $"An error occurred while authenticating: {authenticationException.ErrorCode.ErrorCode}"
                }
            );
        }
    }

    public async Task<UserViewModel?> AuthenticateStored(string username, CancellationToken ct = default)
    {
        try
        {
            var storedUser = _userManager.GetUser(new UserId(Source: ServiceType.Spotify, Id: username));
            if (storedUser.IsNone)
                return null;

            var storedUserVal = storedUser.ValueUnsafe();
            var credentials = new LoginCredentials
            {
                Username = username,
                AuthData = ByteString.FromBase64(storedUserVal.ReusableCredentials),
                Typ = AuthenticationType.AuthenticationStoredSpotifyCredentials
            };
            var cachePath = Path.Combine(
                AppProviders.GetPersistentStoragePath(),
                "Wavee",
                "Spotify"
            );
            Directory.CreateDirectory(cachePath);
            var config = new SpotifyConfig(
                Remote: new SpotifyRemoteConfig(
                    deviceName: storedUserVal.Settings.DeviceName,
                    deviceType: storedUserVal.Settings.DeviceType
                ),
                Cache: new SpotifyCacheConfig(
                    CacheLocation: cachePath,
                    MaxCacheSize: Option<long>.None
                ),
                playback: new SpotifyPlaybackConfig(
                    preferedQuality: storedUserVal.Settings.PreferedQuality,
                    crossfadeDuration: storedUserVal.Settings.CrossfadeSeconds == 0 ? Option<TimeSpan>.None : TimeSpan.FromSeconds(storedUserVal.Settings.CrossfadeSeconds)),
                locale: CultureInfo.CurrentUICulture
            );

            var spotifyClient = new SpotifyClient(Shared.Player, credentials, config);
            var user = await spotifyClient.Metadata.GetMe(ct);
            var welcomeMessage = spotifyClient.WelcomeMessage;
            var updatedUser = new UserViewModel(
                id: new UserId(Source: ServiceType.Spotify, Id: welcomeMessage.CanonicalUsername),
                displayName: user.DisplayName,
                image: user.Images.HeadOrNone().Map(x => x.Url).IfNone(string.Empty),
                client: new WaveeUIClient(spotifyClient))
            {
                ReusableCredentials = welcomeMessage.ReusableAuthCredentials.ToBase64()
            };
            _userManager.SaveUser(updatedUser, false);
            return updatedUser;
        }
        catch (SpotifyAuthenticationException authenticationException)
        {
            throw new MusicAuthenticationException(
                authenticationException.ErrorCode.ErrorCode switch
                {
                    ErrorCode.BadCredentials => "Invalid username or password",
                    _ => $"An error occurred while authenticating: {authenticationException.ErrorCode.ErrorCode}"
                }
            );
        }
    }
}
