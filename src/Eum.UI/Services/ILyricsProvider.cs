using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Models.Users;
using Eum.UI.Items;
using Eum.UI.Services.Tracks;
using Flurl;
using Flurl.Http;
using Refit;
using static Eum.UI.ViewModels.Artists.ArtistRootViewModel;

namespace Eum.UI.Services
{
    public interface ILyricsProvider
    {
        ValueTask<LyricsLine[]?> GetLyrics(ItemId trackId, CancellationToken ct = default);
    }

    public class LyricsProvider : ILyricsProvider
    {
        private readonly ITrackAggregator _trackAggregator;
        private readonly ISpotifyClient _spotifyClient;
        private ConcurrentDictionary<ItemId, LyricsLine[]?> _linesCache =
            new ConcurrentDictionary<ItemId, LyricsLine[]?>();
        public LyricsProvider(ISpotifyClient spotifyClient, ITrackAggregator trackAggregator)
        {
            _spotifyClient = spotifyClient;
            _trackAggregator = trackAggregator;
        }

        public async ValueTask<LyricsLine[]?> GetLyrics(ItemId trackId, CancellationToken ct = default)
        {
            if (_linesCache.TryGetValue(trackId, out var lines)) return lines;
            switch (trackId.Service)
            {
                case ServiceType.Local:
                    return null;
                case ServiceType.Spotify:
                    try
                    {
                        var fetchData = await _spotifyClient.ColorLyrics.GetLyrics(trackId.Id, ct);
                        if (fetchData.Lyrics.SyncType == "LINE_SYNCED")
                        {
                            _linesCache[trackId] = fetchData.Lyrics.Lines;
                            return fetchData.Lyrics.Lines;
                        }
                        else
                        {
                            var appledata = await GetAppleLyrics(new SpotifyId(trackId.Uri), ct);
                            _linesCache[trackId] = appledata;
                            return appledata;
                        }
                    }
                    catch (ApiException x)
                    {
                        if (x.StatusCode == HttpStatusCode.NotFound)
                        {
                            //default to apple?
                            var appledata = await GetAppleLyrics(new SpotifyId(trackId.Uri), ct);
                            _linesCache[trackId] = appledata;
                            return appledata;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    return null;
                case ServiceType.Apple:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private async Task<LyricsLine[]?> GetAppleLyrics(SpotifyId spotifyid, CancellationToken ct = default)
        {
            //fetch the spotify track
            //then get the title + artist,
            var spotifyTrack = await _trackAggregator.GetTrack(new ItemId(spotifyid.Uri), ct);

            return await GetAppleLyrics(spotifyTrack.Name, spotifyTrack.Artists.First().Title, ct);
        }
        private async Task<LyricsLine[]?> GetAppleLyrics(string title, string artist, CancellationToken ct = default)
        {
            //search and get the id
            await using var stream = await "https://eumhelperapi-p4naoifwjq-dt.a.run.app"
                .AppendPathSegments("AppleSearch", "search", $"{title} {artist}")
                .SetQueryParam("language", "en")
                .SetQueryParam("types", "songs")
                .WithOAuthBearerToken((await Ioc.Default.GetRequiredService<IBearerClient>().GetBearerTokenAsync(ct)))
                .GetStreamAsync(cancellationToken: ct);

            using var jsonDocument = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            AppleMusicArtist returnData = default;
            using var songs = jsonDocument
                .RootElement.GetProperty("results")
                .GetProperty("songs")
                .GetProperty("data")
                .EnumerateArray();
            if (songs.Any())
            {
                var song = songs.First();
                var id = song.GetProperty("id").GetString();
                return await GetAppleLyrics(new ItemId($"apple:track:{id}"), ct);
            }

            return null;
        }
        private async Task<LyricsLine[]?> GetAppleLyrics(ItemId appleItemId, CancellationToken ct = default)
        {
            return await "https://eumhelperapi-p4naoifwjq-dt.a.run.app"
                 .AppendPathSegments("AppleSongs", appleItemId.Id, "lyrics")
                 .WithOAuthBearerToken((await Ioc.Default.GetRequiredService<IBearerClient>().GetBearerTokenAsync(ct)))
                 .GetJsonAsync<LyricsLine[]>(cancellationToken: ct);

        }
    }
}
