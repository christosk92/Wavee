using System.Text.Json;
using LanguageExt;
using Spotify.Collection.Proto.V2;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Remote.Contracts;
using Wavee.UI.Core.Contracts.Library;
using static LanguageExt.Prelude;
namespace Wavee.UI.Core.Sys.Live;

internal sealed class SpotifyLibraryClient : ILibraryView
{
    private readonly SpotifyClient _client;
    public SpotifyLibraryClient(SpotifyClient client)
    {
        _client = client;
    }

    public async Task<Unit> SaveItem(Seq<AudioId> ids, bool add, CancellationToken ct = default)
    {
        var userId = _client.WelcomeMessage.CanonicalUsername;
        var groups = ids.GroupBy(c => c.Type);
        foreach (var group in groups)
        {
            var writeRequest = new WriteRequest
            {
                Username = userId,
                Set = group.Key switch
                {
                    AudioItemType.Artist => "artist",
                    AudioItemType.Album or AudioItemType.Track => "collection",
                }
            };

            var addedAt = add ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : 0;
            var removed = !add;
            foreach (var id in group)
            {
                writeRequest.Items.Add(new CollectionItem
                {
                    AddedAt = (int)addedAt,
                    IsRemoved = removed,
                    Uri = id.ToString()
                });
            }

            var _ = await _client.PrivateApi.WriteLibrary(writeRequest, ct);
        }

        return unit;
    }

    public ValueTask<Seq<SpotifyLibaryItem>> FetchTracksAndAlbums(CancellationToken ct = default)
    {
        const string key = "collection";
        var userId = _client.WelcomeMessage.CanonicalUsername;

        return FetchLibraryComponent(_client.Mercury, key, userId, ct)
            .Retry()
            .Run()
            .Map(x => x.Match(
                Succ: y => y,
                Fail: e => throw e
            ));
    }

    public ValueTask<Seq<SpotifyLibaryItem>> FetchArtists(CancellationToken ct = default)
    {
        const string key = "artist";
        var userId = _client.WelcomeMessage.CanonicalUsername;

        return FetchLibraryComponent(_client.Mercury, key, userId, ct)
            .Retry()
            .Run()
            .Map(x => x.Match(
                Succ: y => y,
                Fail: e => throw e
            ));
    }

    public IObservable<SpotifyLibraryUpdateNotification> ListenForChanges => _client.Remote.LibraryChanged;

    private static Aff<Seq<SpotifyLibaryItem>> FetchLibraryComponent(ISpotifyMercuryClient mercury, string key, string userId, CancellationToken ct)
    {
        var aff =
            from data in mercury.Get($"hm://collection/{key}/{userId}?allowonlytracks=false&format=json&", ct).ToAff()
            from parsed in Eff(() =>
            {
                using var doc = JsonDocument.Parse(data.Payload);
                using var items = doc.RootElement.GetProperty("item").EnumerateArray();
                var res = LanguageExt.Seq<SpotifyLibaryItem>.Empty;
                foreach (var item in items)
                {
                    var type = item.GetProperty("type").GetString();
                    // var id = item.GetProperty("identifier").GetString();
                    //id is base64 encoded and needs to be decoded. 
                    var audioId = AudioId.FromRaw(
                        id: item.GetProperty("identifier").GetBytesFromBase64(),
                        type: type switch
                        {
                            "TRACK" => AudioItemType.Track,
                            "ALBUM" => AudioItemType.Album,
                            "ARTIST" => AudioItemType.Artist,
                            _ => throw new Exception("Unknown type")
                        },
                        ServiceType.Spotify
                    );

                    var addedAt = item.GetProperty("added_at").GetInt64();
                    var spotifyLibaryItem = new SpotifyLibaryItem(audioId, DateTimeOffset.FromUnixTimeSeconds(addedAt));
                    res = res.Add(spotifyLibaryItem);
                }


                return res;
            })
            select parsed;

        return aff;
    }
}