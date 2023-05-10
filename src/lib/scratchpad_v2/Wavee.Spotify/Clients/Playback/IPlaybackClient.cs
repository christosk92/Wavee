namespace Wavee.Spotify.Clients.Playback;

public interface IPlaybackClient
{
    Guid Listen(Action<SpotifyPlaybackInfo> onPlaybackInfo);
    
    Task<SpotifyPlaybackInfo> PlayTrack(string uri, CancellationToken ct = default);
}