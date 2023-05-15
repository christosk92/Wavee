using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Channels;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Spotify.Remote.Models;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Remote.Infrastructure;

public sealed class SpotifyRemoteConnection<R> where R : struct, HasWebsocket<R>
{
    public Option<string> LastCommandSentBy { get; private set; }
    public Option<uint> LastCommandId { get; private set; }

    public SpotifyRemoteConnection()
    {
        //setup basic listener
        var listener = Channel.CreateUnbounded<SpotifyWebsocketMessage>();
        var id = AddListener(listener);

        Task.Factory.StartNew(async () =>
        {
            await foreach (var message in listener.Reader.ReadAllAsync())
            {
                //dispatch cluster messages, connection id etc
                HandleMessage(message);
            }
        });

        Task.Factory.StartNew(async () => { });

        ConnectionId = Ref(Option<string>.None);
        LatestCluster = Ref(Option<Cluster>.None);
    }

    private Ref<Option<string>> ConnectionId { get; }
    private Ref<Option<Cluster>> LatestCluster { get; }
    public Option<string> ActualConnectionId => ConnectionId.Value;

    private Atom<HashMap<Guid, Channel<SpotifyWebsocketMessage>>> Listeners =
        Atom(LanguageExt.HashMap<Guid, Channel<SpotifyWebsocketMessage>>.Empty);

    public Guid AddListener(Channel<SpotifyWebsocketMessage> channel)
    {
        var id = Guid.NewGuid();
        Listeners.Swap(listeners => listeners.AddOrUpdate(id, channel));
        return id;
    }


    public Unit SwapConnectionId(string connId)
    {
        atomic(() =>
        {
            ConnectionId.Swap(_ => connId);
            LatestCluster.Swap(_ => Option<Cluster>.None);
        });
        return unit;
    }

    public Unit SwapLatestCluster(Cluster initialCluster)
    {
        atomic(() => { LatestCluster.Swap(_ => initialCluster); });
        return unit;
    }

    public IObservable<Option<Cluster>> OnClusterChange()
        => LatestCluster.OnChange()
            .StartWith(LatestCluster.Value);

    public Unit DispatchMessage(SpotifyWebsocketMessage msg)
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

    private void HandleMessage(SpotifyWebsocketMessage message)
    {
        if (message.Uri.StartsWith("hm://connect-state/v1/cluster"))
        {
            var clusterUpdate = ClusterUpdate.Parser.ParseFrom(message.Payload.ValueUnsafe().Span);
            atomic(() => LatestCluster.Swap(_ => clusterUpdate.Cluster));
        }
        else if (message.Type is SpotifyWebsocketMessageType.Request)
        {
            HandleRequest(message);
        }
    }

    private void HandleRequest(SpotifyWebsocketMessage message)
    {
        using var jsonDcument = JsonDocument.Parse(message.Payload.ValueUnsafe());
        var messageId = jsonDcument.RootElement.GetProperty("message_id").GetUInt32();
        var sentBy = jsonDcument.RootElement.GetProperty("sent_by_device_id").GetString();
        var command = jsonDcument.RootElement.GetProperty("command");
        ReadOnlySpan<char> endpoint = command.GetProperty("endpoint").GetString();
        _ = endpoint switch
        {
            "pause" => HandlePause(messageId, sentBy),
            "resume" => HandleResume(messageId, sentBy),
            "seek_to" => HandleSeekTo(messageId, sentBy, command),
            "skip_next" => HandleSkipNext(messageId, sentBy),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private Unit HandlePause(uint messageId, string sentBy)
    {
        LastCommandId = messageId;
        LastCommandSentBy = sentBy;
        return WaveePlayer.Pause();
    }

    private Unit HandleResume(uint messageId, string sentBy)
    {
        LastCommandId = messageId;
        LastCommandSentBy = sentBy;
        return WaveePlayer.Resume();
    }

    private Unit HandleSkipNext(uint messageId, string sentBy)
    {
        LastCommandId = messageId;
        LastCommandSentBy = sentBy;
        WaveePlayer.SkipNext(true);
        return unit;
    }

    private Unit HandleSeekTo(uint messageId, string sentBy, JsonElement command)
    {
        LastCommandId = messageId;
        LastCommandSentBy = sentBy;
        var value = command.GetProperty("value").GetDouble();
        var ts = TimeSpan.FromMilliseconds(value);
        //100 ms?
        WaveePlayer.Seek(ts, None);
        return unit;
    }
}