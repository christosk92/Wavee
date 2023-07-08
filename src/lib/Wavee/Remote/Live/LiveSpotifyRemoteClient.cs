using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Id;
using Wavee.Infrastructure.Remote;
using Wavee.Playback;
using Wavee.Time;

namespace Wavee.Remote.Live;

internal readonly struct LiveSpotifyRemoteClient : ISpotifyRemoteClient
{
    private readonly Guid _mainConnectionId;
    private readonly TaskCompletionSource<Unit> _waitForConnectionTask;
    private readonly ITimeProvider _timeProvider;

    public LiveSpotifyRemoteClient(Guid mainConnectionId, TaskCompletionSource<Unit> waitForConnectionTask, ITimeProvider timeProvider)
    {
        _mainConnectionId = mainConnectionId;
        _waitForConnectionTask = waitForConnectionTask;
        _timeProvider = timeProvider;
    }

    public IObservable<SpotifyRemoteState> CreateListener()
    {
        _waitForConnectionTask.Task.Wait();
        var reader = _mainConnectionId.CreateListener(RemoteStateListenerCondition);
        LiveSpotifyRemoteClient tmpThis = this;
        return Observable.Create<SpotifyRemoteState>(o =>
        {
            var cancel = new CancellationDisposable();
            Task.Run(async () =>
            {
                try
                {
                    await foreach (var package in reader.Reader.ReadAllAsync(cancel.Token))
                    {

                        var clusterUpdate = ClusterUpdate.Parser.ParseFrom(package.Payload.Span);
                        if (clusterUpdate.Cluster is null)
                        {
                            var state = SpotifyRemoteState.ParseFrom(Option<Cluster>.None, tmpThis._timeProvider);
                            o.OnNext(state ?? new SpotifyRemoteState());
                        }
                        else
                        {
                            var state = SpotifyRemoteState.ParseFrom(clusterUpdate.Cluster, tmpThis._timeProvider);
                            o.OnNext(state!.Value); //Not null because of the if above
                        }
                    }
                }
                finally
                {
                    reader.onDone();
                }
            });
            return cancel;
        }).StartWith(SpotifyRemoteState.ParseFrom(SpotifyRemoteConnection.GetInitialRemoteState(tmpThis._mainConnectionId), _timeProvider) ??
                     new SpotifyRemoteState());
    }

    public IObservable<SpotifyLibraryNotification> CreateLibraryListener()
    {
        _waitForConnectionTask.Task.Wait();
        var reader = _mainConnectionId.CreateListener(LibraryListenerCondition);
        LiveSpotifyRemoteClient tmpThis = this;
        return Observable.Create<SpotifyLibraryNotification>(o =>
        {
            var cancel = new CancellationDisposable();
            Task.Run(async () =>
            {
                try
                {
                    await foreach (var package in reader.Reader.ReadAllAsync(cancel.Token))
                    {
                        var payload = package.Payload;
                        using var jsonDoc = JsonDocument.Parse(payload);
                        using var rootArr = jsonDoc.RootElement.EnumerateArray();


                        var addedItems = new List<SpotifyLibraryItem>();
                        var removedItems = new List<SpotifyLibraryItem>();

                        foreach (var rootItemStr in rootArr.Select(c => c.ToString()))
                        {
                            using var rootItem = JsonDocument.Parse(rootItemStr);
                            using var items = rootItem.RootElement.GetProperty("items").EnumerateArray();
                            foreach (var item in items)
                            {
                                var type = item.GetProperty("type").GetString();
                                var removed = item.GetProperty("removed").GetBoolean();
                                var addedAt = item.GetProperty("addedAt").GetUInt64();
                                if (!removed)
                                {
                                    addedItems.Add(new SpotifyLibraryItem(
                                        Id: SpotifyId.FromBase62(
                                            base62: item.GetProperty("identifier").GetString().AsSpan(),
                                            itemType: type switch
                                            {
                                                "track" => AudioItemType.Track,
                                                "artist" => AudioItemType.Artist,
                                                "album" => AudioItemType.Album,
                                                _ => throw new ArgumentOutOfRangeException()
                                            }, ServiceType.Spotify),
                                        AddedAt: DateTimeOffset.FromUnixTimeSeconds((long)addedAt)));
                                }
                                else
                                {
                                    removedItems.Add(new SpotifyLibraryItem(
                                        Id: SpotifyId.FromBase62(
                                            base62: item.GetProperty("identifier").GetString().AsSpan(),
                                            itemType: type switch
                                            {
                                                "track" => AudioItemType.Track,
                                                "artist" => AudioItemType.Artist,
                                                "album" => AudioItemType.Album,
                                                _ => throw new ArgumentOutOfRangeException()
                                            }, ServiceType.Spotify),
                                        AddedAt: Option<DateTimeOffset>.None));
                                }
                            }
                        }

                        if (addedItems.Count > 0)
                        {
                            o.OnNext(new SpotifyLibraryNotification(addedItems.ToSeq(), true));
                        }

                        if (removedItems.Count > 0)
                        {
                            o.OnNext(new SpotifyLibraryNotification(removedItems.ToSeq(), false));
                        }
                    }
                }
                finally
                {
                    reader.onDone();
                }
            });
            return cancel;
        });
    }


    public Option<SpotifyRemoteState> LatestState
    {
        get
        {
            var parsed = SpotifyRemoteState.ParseFrom(SpotifyRemoteConnection.GetInitialRemoteState(_mainConnectionId), _timeProvider);
            if (parsed.HasValue)
                return parsed.Value;
            return Option<SpotifyRemoteState>.None;
        }
    }

    public IObservable<Unit> CreatePlaylistListener()
    {

        _waitForConnectionTask.Task.Wait();
        var reader = _mainConnectionId.CreateListener(PlaylistlistenerCondition);
        LiveSpotifyRemoteClient tmpThis = this;
        return Observable.Create<Unit>(o =>
        {
            var cancel = new CancellationDisposable();
            Task.Run(async () =>
            {
                try
                {
                    await foreach (var package in reader.Reader.ReadAllAsync(cancel.Token))
                    {
                        o.OnNext(Unit.Default);
                    }
                }
                finally
                {
                    reader.onDone();
                }
            });
            return cancel;
        });
    }

    private static bool RemoteStateListenerCondition(SpotifyRemoteMessage packagetocheck)
    {
        return packagetocheck.Uri.StartsWith("hm://connect-state/v1/cluster");
    }

    private static bool PlaylistlistenerCondition(SpotifyRemoteMessage message)
    {
        //hm://playlist/v2/user/7ucghdgquf6byqusqkliltwc2/rootlist
        //regex hm://playlist/v2/user/.*?/.*?/rootlist
        var regex = new Regex(@"hm://playlist/v2/user/.*?/.*?/rootlist");
        var isMathc = regex.IsMatch(message.Uri);
        return isMathc;
    }
    private static bool LibraryListenerCondition(SpotifyRemoteMessage packagetocheck)
    {
        return packagetocheck.Uri.StartsWith("hm://collection/") && packagetocheck.Uri.EndsWith("/json");
    }
    internal async Task PlaybackStateUpdated(SpotifyLocalPlaybackState spotifyLocalPlaybackState)
    {
        await SpotifyRemoteConnection.PutState(_mainConnectionId, spotifyLocalPlaybackState);
    }

    public void Dispose()
    {
        SpotifyRemoteConnection.Dispose(_mainConnectionId);
    }
}

internal readonly record struct SpotifyCommand(uint MessageId, string SentByDeviceId, string Endpoint,
    ReadOnlyMemory<byte> Data);