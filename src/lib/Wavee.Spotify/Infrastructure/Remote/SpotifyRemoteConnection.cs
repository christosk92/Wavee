using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Channels;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Core.Player;
using Wavee.Spotify.Infrastructure.Remote.Messaging;

namespace Wavee.Spotify.Infrastructure.Remote;

internal sealed class SpotifyRemoteConnection
{
    private Atom<HashMap<Guid, Channel<SpotifyWebsocketMessage>>> Listeners =
        Atom(LanguageExt.HashMap<Guid, Channel<SpotifyWebsocketMessage>>.Empty);

    private readonly Subject<SpotifyLibraryUpdateNotification> _libraryNotifSubj;
    private readonly Subject<SpotifyRootlistUpdateNotification> _rootlistNotifSubj;
    private readonly string _userId;

    private Ref<Option<string>> _connectionId { get; }
    internal Ref<Option<Cluster>> _latestCluster { get; }

    public SpotifyRemoteConnection(string userId)
    {
        _userId = userId;
        _rootlistNotifSubj = new Subject<SpotifyRootlistUpdateNotification>();
        _libraryNotifSubj = new Subject<SpotifyLibraryUpdateNotification>();
        var listener = Channel.CreateUnbounded<SpotifyWebsocketMessage>();
        var id = AddListener(listener);

        Task.Factory.StartNew(async () =>
        {
            await foreach (var message in listener.Reader.ReadAllAsync())
            {
                //dispatch cluster messages, connection id etc
                try
                {
                    await HandleMessage(message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        });

        _connectionId = Ref(Option<string>.None);
        _latestCluster = Ref(Option<Cluster>.None);
    }

    public Option<string> LastCommandSentBy { get; private set; }
    public Option<uint> LastCommandId { get; private set; }
    public Option<string> ConnectionId => _connectionId.Value;

    public IObservable<SpotifyRootlistUpdateNotification> OnRootListNotification => _rootlistNotifSubj.StartWith(
        new SpotifyRootlistUpdateNotification(_userId));

    public IObservable<SpotifyLibraryUpdateNotification> OnLibraryNotification => _libraryNotifSubj
        .StartWith(new SpotifyLibraryUpdateNotification(
            true, new AudioId(), true, Option<DateTimeOffset>.None));

    public IObservable<Option<Cluster>> OnClusterChange()
        => _latestCluster.OnChange()
            .StartWith(_latestCluster.Value);


    internal Unit DispatchMessage(SpotifyWebsocketMessage msg)
    {
        var listeners = Listeners.Value;
        foreach (var listener in listeners)
        {
            if (!listener.Value.Writer.TryWrite(msg))
            {
                //remove listener
                Listeners.Swap(x => x.Remove(listener.Key));
            }
        }

        return unit;
    }

    internal Guid AddListener(Channel<SpotifyWebsocketMessage> channel)
    {
        var id = Guid.NewGuid();
        Listeners.Swap(listeners => listeners.AddOrUpdate(id, channel));
        return id;
    }

    internal Unit SwapConnectionId(string connId)
    {
        atomic(() =>
        {
            _connectionId.Swap(_ => connId);
            _latestCluster.Swap(_ => Option<Cluster>.None);
        });
        return unit;
    }

    internal Unit SwapLatestCluster(Cluster initialCluster)
    {
        atomic(() => { _latestCluster.Swap(_ => initialCluster); });
        return unit;
    }

    private async Task HandleMessage(SpotifyWebsocketMessage message)
    {
        if (message.Type is SpotifyWebsocketMessageType.ConnectionId)
        {
            //  atomic(() => _connectionId.Swap(_ => ))
            Debugger.Break();
            return;
        }
        if (message.Uri.StartsWith("hm://connect-state/v1/cluster"))
        {
            var clusterUpdate = ClusterUpdate.Parser.ParseFrom(message.Payload.ValueUnsafe().Span);
            atomic(() => _latestCluster.Swap(_ => clusterUpdate.Cluster));
            GC.Collect();
        }
        else if (message.Uri.Equals($"hm://playlist/v2/user/{_userId}/rootlist"))
        {
            atomic(() => _rootlistNotifSubj.OnNext(new SpotifyRootlistUpdateNotification(_userId)));
        }
        else if (message.Uri.StartsWith("hm://collection/") && message.Uri.EndsWith("/json"))
        {
            var payload = message.Payload.ValueUnsafe();
            using var jsonDoc = JsonDocument.Parse(payload);
            using var rootArr = jsonDoc.RootElement.EnumerateArray();
            foreach (var rootItemStr in rootArr.Select(c => c.ToString()))
            {
                using var rootItem = JsonDocument.Parse(rootItemStr);
                using var items = rootItem.RootElement.GetProperty("items").EnumerateArray();
                foreach (var item in items)
                {
                    var type = item.GetProperty("type").GetString();
                    var removed = item.GetProperty("removed").GetBoolean();
                    var addedAt = item.GetProperty("addedAt").GetUInt64();
                    var result = new SpotifyLibraryUpdateNotification(
                        Initial: false,
                        Item: AudioId.FromBase62(
                            base62: item.GetProperty("identifier").GetString(),
                            itemType: type switch
                            {
                                "track" => AudioItemType.Track
                            }, ServiceType.Spotify),
                        Removed: removed,
                        AddedAt: removed ? Option<DateTimeOffset>.None : DateTimeOffset.Now
                    );
                    atomic(() => _libraryNotifSubj.OnNext(result));
                }
            }
        }
        else if (message.Type is SpotifyWebsocketMessageType.Request)
        {
            await HandleRequest(message);
        }
    }

    private async Task HandleRequest(SpotifyWebsocketMessage message)
    {
        using var jsonDcument = JsonDocument.Parse(message.Payload.ValueUnsafe());
        var messageId = jsonDcument.RootElement.GetProperty("message_id").GetUInt32();
        var sentBy = jsonDcument.RootElement.GetProperty("sent_by_device_id").GetString();
        var command = jsonDcument.RootElement.GetProperty("command");
        var endpoint = command.GetProperty("endpoint").GetString();
        LastCommandId = messageId;
        LastCommandSentBy = sentBy;
        _ = await (endpoint switch
        {
            "pause" => HandlePause(),
            "resume" => HandleResume(),
            "seek_to" => HandleSeekTo(command),
            "skip_next" => HandleSkipNext(),
            _ => throw new ArgumentOutOfRangeException()
        });
    }

    private ValueTask<Unit> HandlePause()
    {
        return WaveePlayer.Pause();
    }

    private ValueTask<Unit> HandleResume()
    {
        return WaveePlayer.Resume();
    }

    private async ValueTask<Unit> HandleSkipNext()
    {
        await WaveePlayer.SkipNext();
        return unit;
    }

    private ValueTask<Unit> HandleSeekTo(JsonElement command)
    {
        var value = command.GetProperty("value").GetDouble();
        var ts = TimeSpan.FromMilliseconds(value);
        return WaveePlayer.Seek(ts, None);
    }
}