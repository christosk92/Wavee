using Wavee.Core.Extensions;
using Wavee.Spotify.Http.Clients;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;
using Wavee.Spotify.Playback;

namespace Wavee.Spotify;

public sealed class SpotifyClient : ISpotifyClient
{
    private readonly IAPIConnector _apiConnector;

    public SpotifyClient(SpotifyClientConfig config)
    {
        Guard.NotNull(nameof(config), config);
        if (config.Authenticator == null)
        {
#pragma warning disable CA2208
            // ReSharper disable once NotResolvedInText
            throw new ArgumentNullException(
                "Authenticator in config is null. Please supply it via `WithAuthenticator` or `WithToken`");
#pragma warning restore CA2208
        }

        Cache = config.Cache;
        _apiConnector = config.BuildApiConnector();
        _apiConnector.ResponseReceived += (sender, response) => { LastResponse = response; };
        UserProfile = new UserProfileClient(_apiConnector);
        Tracks = new TracksClient(_apiConnector);
        Episodes = new EpisodesClient(_apiConnector);
        ((ISpotifyClient)this).Context = new ContextClient(_apiConnector);
        ((ISpotifyClient)this).AudioKey = new AudioKeyClient(config.Authenticator, Cache);
        ((ISpotifyClient)this).Cdn = new CdnClient(_apiConnector);
        Player = new PlayerClient(
            this,
            config.Player,
            _apiConnector,
            config.DeviceId,
            config.Authenticator);
    }

    public ISpotifyCache Cache { get; }
    public IPlayerClient Player { get; }
    public IUserProfileClient UserProfile { get; }
    public ITracksClient Tracks { get; }
    public IEpisodesClient Episodes { get; }
    IContextClient ISpotifyClient.Context { get; set; }
    IAudioKeyClient ISpotifyClient.AudioKey { get; set; }
    ICdnClient ISpotifyClient.Cdn { get; set; }
    public IResponse? LastResponse { get; private set; }

    internal static Func<SpotifyEncryptedStream, byte[], ISpotifyDecryptedStream>? DecryptionFactory { get; set; }
}

public interface ISpotifyClient
{
    ISpotifyCache Cache { get; }
    IPlayerClient Player { get; }
    IUserProfileClient UserProfile { get; }
    ITracksClient Tracks { get; }
    IEpisodesClient Episodes { get; }
    internal IContextClient Context { get; set; }
    internal IAudioKeyClient AudioKey { get; set; }
    internal ICdnClient Cdn { get; set; }
}