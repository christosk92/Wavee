using System.Reactive.Linq;
using System.Text.Json;
using LanguageExt;
using ReactiveUI;
using Wavee.Id;
using Wavee.Remote;
using Wavee.Token.Live;
using Wavee.UI.Client.Home;
using static LanguageExt.Prelude;
namespace Wavee.UI.Client.Library;

internal sealed class SpotifyUILibraryClient : IWaveeUILibraryClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;
    public SpotifyUILibraryClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public IObservable<WaveeUILibraryNotification> CreateListener()
    {
        if (!_spotifyClient.TryGetTarget(out var client))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        return client.Remote.CreateLibraryListener()
            .Select((x) => new WaveeUILibraryNotification(
                Ids: x.Id.Select(f => new WaveeUILibraryItem(
                    Id: f.ToString(),
                    Type: f.Id.Type,
                    Source: f.Id.Service,
                    AddedAt: f.AddedAt
                )), x.Added
            ))
            .ObserveOn(RxApp.TaskpoolScheduler);
    }

    public async Task<WaveeUILibraryNotification> InitializeLibraryAsync(CancellationToken cancellationToken)
    {
        if (!_spotifyClient.TryGetTarget(out var client))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        var tracksAndAlbums = FetchTracksAndAlbums(client, cancellationToken);
        var artists = FetchArtists(client, cancellationToken);
        var results = await Task.WhenAll(tracksAndAlbums.AsTask(), artists.AsTask());
        return new WaveeUILibraryNotification(
            Ids: results.SelectMany(c => c
                .Select(x => new WaveeUILibraryItem(
                    Id: x.Id.ToString(),
                    Type: x.Id.Type,
                    Source: x.Id.Service,
                    AddedAt: x.AddedAt
                ))).ToSeq(),
            Added: true
        );
    }
    public ValueTask<Seq<SpotifyLibraryItem>> FetchTracksAndAlbums(SpotifyClient spotifyClient,
        CancellationToken ct = default)
    {
        const string key = "collection";
        var userId = spotifyClient.WelcomeMessage.CanonicalUsername;

        return FetchLibraryComponent(spotifyClient.Mercury, key, userId, ct)
            .Retry()
            .Run()
            .Map(x => x.Match(
                Succ: y => y,
                Fail: e => throw e
            ));
    }

    public ValueTask<Seq<SpotifyLibraryItem>> FetchArtists(SpotifyClient spotifyClient, CancellationToken ct = default)
    {
        const string key = "artist";
        var userId = spotifyClient.WelcomeMessage.CanonicalUsername;

        return FetchLibraryComponent(spotifyClient.Mercury, key, userId, ct)
            .Retry()
            .Run()
            .Map(x => x.Match(
                Succ: y => y,
                Fail: e => throw e
            ));
    }

    private static Aff<Seq<SpotifyLibraryItem>> FetchLibraryComponent(IMercuryClient mercury, string key, string userId, CancellationToken ct)
    {
        var aff =
            from data in mercury.Get($"hm://collection/{key}/{userId}?allowonlytracks=false&format=json&", ct).ToAff()
            from parsed in Eff(() =>
            {
                using var doc = JsonDocument.Parse(data.Payload);
                using var items = doc.RootElement.GetProperty("item").EnumerateArray();
                var res = LanguageExt.Seq<SpotifyLibraryItem>.Empty;
                foreach (var item in items)
                {
                    var type = item.GetProperty("type").GetString();
                    // var id = item.GetProperty("identifier").GetString();
                    //id is base64 encoded and needs to be decoded. 
                    var audioId = SpotifyId.FromRaw(
                        id: item.GetProperty("identifier").GetBytesFromBase64(),
                        type: type switch
                        {
                            "TRACK" or "LOCAL_TRACK" => AudioItemType.Track,
                            "ALBUM" => AudioItemType.Album,
                            "ARTIST" => AudioItemType.Artist,
                            _ => throw new Exception("Unknown type")
                        },
                        !type.StartsWith("LOCAL_") ? ServiceType.Spotify : ServiceType.Local
                    );

                    var addedAt = item.GetProperty("added_at").GetInt64();
                    var spotifyLibaryItem = new SpotifyLibraryItem(audioId, DateTimeOffset.FromUnixTimeSeconds(addedAt));
                    res = res.Add(spotifyLibaryItem);
                }


                return res;
            })
            select parsed;

        return aff;
    }

}