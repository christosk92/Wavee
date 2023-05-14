using System.Reactive.Linq;
using System.Threading.Channels;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Remote.Models;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Remote.Infrastructure;

public sealed class SpotifyRemoteConnection<R> where R : struct, HasWebsocket<R>
{
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
        
        Task.Factory.StartNew(async () =>
        {
            
        });

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
    }
}