using LanguageExt;
using Wavee.Spotify;
using Wavee.UI.Core.Contracts.Artist;
using Wavee.UI.Core.Contracts.Home;
using Wavee.UI.Core.Contracts.Metadata;
using Wavee.UI.Core.Contracts.Playback;
using Wavee.UI.Core.Sys.Mock;

namespace Wavee.UI.Core.Sys.Live;

internal sealed class LiveAppState : IAppState
{
    private readonly SpotifyClient _client;

    public LiveAppState(SpotifyClient client)
    {
        _client = client;

        UserSettings = new UserSettings(client.WelcomeMessage.CanonicalUsername);
    }

    public UserProfile UserProfile => new UserProfile(
        Id: _client.WelcomeMessage.CanonicalUsername,
        Name: _client.WelcomeMessage.CanonicalUsername,
        ImageUrl: Option<string>.None
    );

    public UserSettings UserSettings { get; }
    public IHomeView Home => new SpotifyHomeClient(_client);
    public IArtistView Artist => new SpotifyArtistClient(_client);
    public IRemotePlaybackClient Remote => new SpotifyRemoteClient(_client);
    public IMetadataClient Metadata => new SpotifyMetadataClient(_client);
    public string DeviceId => _client.DeviceId;
}