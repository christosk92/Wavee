using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using Eum.Spotify.playlist4;
using LanguageExt;
using ReactiveUI;
using System.Text.Json;
using Wavee.Core.Ids;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibrariesViewModel : ReactiveObject
{
    private readonly SourceCache<SpotifyLibaryItem, AudioId> _items = new(s => s.Id);

    public LibrariesViewModel(
        Action<Seq<AudioId>> onLibraryItemAdded,
        Action<Seq<AudioId>> onLibraryItemRemoved, string userId)
    {
        var listener = _items.Connect()
            .Transform(x => x.Id)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                var grouped = x.GroupBy(y => y.Reason);
                foreach (var y in grouped)
                {
                    switch (y.Key)
                    {
                        case ChangeReason.Add:
                            onLibraryItemAdded(y.Map(z=> z.Key).ToSeq());
                            break;
                        case ChangeReason.Remove:
                            onLibraryItemRemoved(y.Map(z => z.Key).ToSeq());
                            break;
                    }
                }
            });

        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var initial = await FetchLibraryInitial(userId);
                    _items.AddOrUpdate(initial);
                    break;
                }
                catch (Exception e)
                {
                    //"   at Wavee.UI.ViewModels.Library.LibrariesViewModel.<FetchLibraryComponent>d__3.MoveNext() in C:\\Users\\chris-pc\\dev\\personal\\Wavee\\scratchpad\\Wavee2Lib\\UI\\Wavee.UI\\ViewModels\\Library\\LibrariesViewModel.cs:line 90\r\n   at Wavee.UI.ViewModels.Library.LibrariesViewModel.<FetchLibraryInitial>d__2.MoveNext() in C:\\Users\\chris-pc\\dev\\personal\\Wavee\\scratchpad\\Wavee2Lib\\UI\\Wavee.UI\\ViewModels\\Library\\LibrariesViewModel.cs:line 65\r\n   at Wavee.UI.ViewModels.Library.LibrariesViewModel.<>c__DisplayClass1_0.<<-ctor>b__2>d.MoveNext() in C:\\Users\\chris-pc\\dev\\personal\\Wavee\\scratchpad\\Wavee2Lib\\UI\\Wavee.UI\\ViewModels\\Library\\LibrariesViewModel.cs:line 46"
                    Console.WriteLine(e);
                    Debug.WriteLine(e);
                    await Task.Delay(4000);
                }
            }
        });
    }

    private static async Task<Seq<SpotifyLibaryItem>> FetchLibraryInitial(string userId, CancellationToken ct = default)
    {
        //hm://collection/collection/{id}?format=json
        var tracksAndAlbums = FetchLibraryComponent("collection", userId, ct);
        var artists = FetchLibraryComponent("artist", userId, ct);
        var results = await Task.WhenAll(tracksAndAlbums, artists);
        return results.SelectMany(c => c).ToSeq();
    }

    private static async Task<Seq<SpotifyLibaryItem>> FetchLibraryComponent(string key, string userId, CancellationToken ct)
    {
        var libraryAff = State.Instance.Client.GetLibarryComponent(key, userId, ct);

        var response = await libraryAff.Run();
        var k = response.ThrowIfFail();
        using var doc = JsonDocument.Parse(k.Payload);
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
                    "TRACK" or "LOCAL_TRACK" => AudioItemType.Track,
                    "ALBUM" => AudioItemType.Album,
                    "ARTIST" => AudioItemType.Artist,
                    _ => throw new Exception("Unknown type")
                },
                type is "LOCAL_TRACK" ? ServiceType.Local : ServiceType.Spotify
            );
// #if DEBUG
//             if (type is "LOCAL_TRACK")
//                 Debugger.Break();
// #endif
            var addedAt = item.GetProperty("added_at").GetInt64();
            var spotifyLibaryItem = new SpotifyLibaryItem(audioId, DateTimeOffset.FromUnixTimeSeconds(addedAt));
            res = res.Add(spotifyLibaryItem);
        }


        return res;
    }
    public bool InLibrary(AudioId id)
    {
        return _items.Lookup(id).HasValue;
    }
}
public enum LibraryItemType
{
    Songs,
    Albums,
    Artists
}

public readonly record struct SpotifyLibaryItem(AudioId Id, DateTimeOffset AddedAt);