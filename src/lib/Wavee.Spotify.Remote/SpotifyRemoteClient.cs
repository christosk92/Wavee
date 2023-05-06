using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.Effects.Traits;
using Wavee.Spotify.Remote.Infrastructure.State;
using Wavee.Spotify.Remote.Infrastructure.State.Messages;

namespace Wavee.Spotify.Remote;

internal sealed class SpotifyRemoteClient<RT> : ISpotifyRemoteClient where RT : struct, HasCancel<RT>
{
    private readonly Ref<SpotifyRemoteState> _remoteState;
    private readonly Ref<Option<Cluster>> _cluster;
    private readonly Func<Task<string>> _getBearer;

    private readonly RT _runtime;

    public SpotifyRemoteClient(SpotifyRemoteState newRemoteState,
        Cluster cluster,
        Func<Task<string>> getBearer, RT runtime)
    {
        _remoteState = Ref(newRemoteState);
        _cluster = Ref(Some(cluster));
        _getBearer = getBearer;
        _runtime = runtime;
    }

    public Option<Cluster> Cluster => _cluster.Value;
    public IObservable<Option<Cluster>> ClusterUpdated => _cluster.OnChange();

    internal Unit OnCluster(ClusterUpdate parseFrom)
    {
        atomic(() => _cluster.Swap(_ => Some(parseFrom.Cluster)));
        atomic(() => _remoteState.Swap(s => s.FromCluster(parseFrom.Cluster)));
        return unit;
    }

    public Aff<RT, bool> OnRequest(string key, SpotifyRequestMessage spotifyRequestCommand) => Aff<RT, bool>((rt) =>
    {
        throw new NotImplementedException();
    });
}