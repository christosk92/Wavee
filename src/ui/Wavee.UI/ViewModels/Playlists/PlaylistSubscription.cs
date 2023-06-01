using System.Reactive.Linq;
using DynamicData;
using Eum.Spotify.playlist4;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Services;

namespace Wavee.UI.ViewModels.Playlists;

public sealed class PlaylistSubscription : ReactiveObject
{
    private readonly IDisposable _listener;

    private readonly SourceCache<(Item Item, int Index), ByteString> _items = new(s => s.Item.Attributes.ItemId);

    public PlaylistSubscription(string id,
        WaveeUIRuntime runtime,
        bool isFolder = false)
    {
        Id = id;
        //setup a listener
        IsFolder = isFolder;
        if (!isFolder)
        {
            _listener =
                Spotify<WaveeUIRuntime>
                    .ObservePlaylist(AudioId.FromUri(id))
                    .Run(runtime)
                    .ThrowIfFail()
                    .ValueUnsafe()
                    .SelectMany(async x =>
                    {
                        //if add: fetc from cache
                        var tracksToFetch = x.Ops.Where<Op>(op => op.Kind == Op.Types.Kind.Add)
                            .SelectMany(op => op.Add.Items)
                            .Select(x => AudioId.FromUri(x.Uri))
                            .Where(c => !Cache.ContainsKey(c))
                            .ToSeq();
                        if (tracksToFetch.Count > 0)
                        {
                            var tracks = await TrackEnqueueService<WaveeUIRuntime>.GetTracks(tracksToFetch);
                            foreach (var track in tracks)
                            {
                                Cache[track.Key] = track.Value;
                            }
                        }

                        return x;
                    })
                    .Select(c =>
                    {
                        _items.Edit(updater =>
                        {
                            var oldList = updater.Items.ToList();
                            foreach (var op in c.Ops)
                            {
                                switch (op.Kind)
                                {
                                    case Op.Types.Kind.Add:
                                        var projected = op.Add.Items.Select((f, i) => (f, i));
                                        if (op.Add.AddFirst)
                                        {
                                            oldList.InsertRange(0, projected);
                                        }
                                        else if (op.Add.AddLast)
                                        {
                                            oldList.AddRange(projected);
                                        }
                                        else if (op.Add.HasFromIndex)
                                        {
                                            oldList.InsertRange(op.Add.FromIndex, projected);
                                        }

                                        break;
                                    case Op.Types.Kind.Rem:
                                        if (op.Rem.HasFromIndex)
                                        {
                                            oldList.RemoveRange(op.Rem.FromIndex, op.Rem.Length);
                                        }
                                        else
                                        {
                                            oldList.RemoveRange(0, op.Rem.Length);
                                        }

                                        break;
                                    case Op.Types.Kind.Mov:
                                        break;
                                    case Op.Types.Kind.UpdateItemAttributes:
                                        break;
                                    case Op.Types.Kind.UpdateListAttributes:
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }

                            updater.Load(oldList);
                        });
                        // GC.Collect();
                        return unit;
                    })
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Subscribe();
        }
    }

    private static readonly Dictionary<AudioId, Option<TrackOrEpisode>> Cache = new();
    public required bool IsInFolder { get; set; }
    public required bool IsFolder { get; set; }
    public required string Id { get; set; }
    public required string OwnerId { get; set; }
    public required Seq<PlaylistSubscription> SubItems { get; set; }
    public required ByteString Revision { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public int Index { get; set; }
    public string Name { get; set; }
    public void AddSubitem(PlaylistSubscription playlistViewModel)
    {
        SubItems = SubItems.Add(playlistViewModel);
    }

    public void Destroy()
    {
        _listener.Dispose();
        _items.Clear();
        _items.Dispose();

        foreach (var subItem in SubItems)
        {
            subItem.Destroy();
        }
    }

    public void OnNavigatedTo(object? parameter)
    {

    }
}