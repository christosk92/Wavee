using ReactiveUI;
using System.Reactive.Linq;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using DynamicData;
using System.Text.Json;
using System.Windows.Input;
using LanguageExt;
using Wavee.Core.Ids;
using Spotify.Collection.Proto.V2;

namespace Wavee.UI.ViewModels.Library;

public readonly record struct ModifyLibraryCommand(Seq<AudioId> Ids, bool Add);
public sealed class LibraryViewModel<R> : ReactiveObject where R : struct, HasSpotify<R>
{
    //private readonly ReadOnlyObservableCollection<SpotifyLibaryItem> _libraryItemsView;
    private readonly string _userId;
    private readonly SourceCache<SpotifyLibaryItem, AudioId> _items = new(s => s.Id);
    public LibraryViewModel(R runtime,
        Action<Seq<AudioId>> onLibraryItemAdded,
        Action<Seq<AudioId>> onLibraryItemRemoved, string userId)
    {
        _userId = userId;
        SaveCommand = ReactiveCommand.CreateFromTask<ModifyLibraryCommand>(async cmd =>
        {
            var groups = cmd.Ids.GroupBy(c => c.Type);
            foreach (var group in groups)
            {
                var writeRequest = new WriteRequest
                {
                    Username = _userId,
                    Set = group.Key switch
                    {
                        AudioItemType.Artist => "artist",
                        AudioItemType.Album or AudioItemType.Track => "collection",
                    }
                };

                var addedAt = cmd.Add ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : 0;
                var removed = !cmd.Add;
                foreach (var id in group)
                {
                    writeRequest.Items.Add(new CollectionItem
                    {
                        AddedAt = (int)addedAt,
                        IsRemoved = removed,
                        Uri = id.ToString()
                    });
                }

                var writeResponse = await Spotify<R>.WriteLibrary(writeRequest)
                    .Run(runtime);
            }

        });

        var libraryObservable = Spotify<R>.ObserveLibrary()
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe()
            //.Throttle(TimeSpan.FromMilliseconds(50))
            .SelectMany(async c =>
            {
                if (c.Initial)
                {
                    var items = await FetchLibraryInitial(
                        runtime,
                        _userId,
                        CancellationToken.None);
                    return (items, true);
                }

                if (c.Removed)
                {
                    return (Seq1(new SpotifyLibaryItem(
                        Id: c.Item,
                        AddedAt: DateTimeOffset.MinValue
                    )), false);
                }

                var newItem = new SpotifyLibaryItem(
                    Id: c.Item,
                    AddedAt: c.AddedAt.ValueUnsafe()
                );
                return (Seq1(newItem), true);
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(itemsToAddOrRemove =>
            {
                _items.Edit(innerList =>
                {
                    if (itemsToAddOrRemove.Item2)
                    {
                        innerList.AddOrUpdate(itemsToAddOrRemove.Item1);
                        onLibraryItemAdded(itemsToAddOrRemove.Item1.Select(c => c.Id));
                    }
                    else
                    {
                        innerList.Remove(itemsToAddOrRemove.Item1);
                        onLibraryItemRemoved(itemsToAddOrRemove.Item1.Select(c => c.Id));
                    }
                });
            });
    }

    public IEnumerable<SpotifyLibaryItem> GetLibraryItems()
    {
        return _items.Items;
    }
    public IObservable<IChangeSet<SpotifyLibaryItem, AudioId>> Items => _items.Connect();
    public ICommand SaveCommand { get; }

    private static async Task<Seq<SpotifyLibaryItem>> FetchLibraryInitial(R runtime,
        string userId,
        CancellationToken ct = default)
    {
        //hm://collection/collection/{id}?format=json
        var tracksAndAlbums = FetchLibraryComponent(runtime, "collection", userId, ct);
        var artists = FetchLibraryComponent(runtime, "artist", userId, ct);
        var results = await Task.WhenAll(tracksAndAlbums, artists);
        return results.SelectMany(c => c).ToSeq();
    }

    private static async Task<Seq<SpotifyLibaryItem>> FetchLibraryComponent(
        R runtime,
        string key, string userId, CancellationToken ct)
    {
        var aff =
            from mercury in Spotify<R>.Mercury()
            from tracksAndAlbums in mercury.Get(
                $"hm://collection/{key}/{userId}?allowonlytracks=false&format=json&",
                ct).ToAff()
            select tracksAndAlbums;

        var response = await aff.Run(runtime: runtime);
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