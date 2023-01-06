using System.Collections.Concurrent;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.UI.Items;

namespace Eum.UI.Services
{
    public interface ILyricsProvider
    {
        ValueTask<LyricsLine[]?> GetLyrics(ItemId trackId, CancellationToken ct = default);
    }

    public class LyricsProvider : ILyricsProvider
    {
        private readonly ISpotifyClient _spotifyClient;
        private ConcurrentDictionary<ItemId, LyricsLine[]> _linesCache =
            new ConcurrentDictionary<ItemId, LyricsLine[]>();
        public LyricsProvider(ISpotifyClient spotifyClient)
        {
            _spotifyClient = spotifyClient;
        }

        public async ValueTask<LyricsLine[]?> GetLyrics(ItemId trackId, CancellationToken ct = default)
        {
            if (_linesCache.TryGetValue(trackId, out var lines)) return lines;
            switch (trackId.Service)
            {
                case ServiceType.Local:
                    return null;
                case ServiceType.Spotify:
                    var fetchData = await _spotifyClient.ColorLyrics.GetLyrics(trackId.Id, ct);
                    if (fetchData.Lyrics.SyncType == "LINE_SYNCED")
                    {
                        _linesCache[trackId] = fetchData.Lyrics.Lines;
                        return fetchData.Lyrics.Lines;
                    }
                    return null;
                case ServiceType.Apple:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
