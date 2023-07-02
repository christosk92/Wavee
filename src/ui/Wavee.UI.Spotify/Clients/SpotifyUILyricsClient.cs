using Wavee.Id;
using Wavee.Metadata;
using Wavee.UI.Client.Lyrics;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyUILyricsClient : IWaveeUILyricsClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;
    public SpotifyUILyricsClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public ValueTask<LyricsLine[]> GetLyrics(string trackId, CancellationToken ct)
    {
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        return spotifyClient.Metadata.GetLyrics(SpotifyId.FromUri(trackId), ct);
    }

}