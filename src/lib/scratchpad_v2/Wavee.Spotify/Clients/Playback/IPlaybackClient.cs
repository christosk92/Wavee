using Wavee.Spotify.Configs;

namespace Wavee.Spotify.Clients.Playback;

public interface IPlaybackClient
{
    Guid Listen(Action<SpotifyPlaybackInfo> onPlaybackInfo);

    Task<SpotifyPlaybackInfo> PlayTrack(string uri,
        Option<PreferredQualityType> preferredQualityOverride,
        CancellationToken ct = default);

    Task<bool> Pause(CancellationToken ct = default);
    Task<bool> Seek(TimeSpan to, CancellationToken ct = default);
}