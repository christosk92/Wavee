using System.Reactive.Subjects;
using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Remote.State;

public sealed class SpotifyRemoteInfo
{
    internal LanguageExt.Ref<Option<string>> SpotifyConnectionId = Ref<Option<string>>(None);
    internal LanguageExt.Ref<Cluster> Cluster = Ref<Cluster>(new Cluster());
    internal Subject<Seq<SpotifyCollectionUpdateNotificationItem>> Collection = new();

    public Option<Cluster> ClusterInfo => Cluster.Value;
    public Option<string> ConnectionId => SpotifyConnectionId.Value;

    public IObservable<Cluster> ClusterChanged => Cluster.OnChange();
    public IObservable<Seq<SpotifyCollectionUpdateNotificationItem>> CollectionChanged => Collection;
    internal void UpdateCluster(Cluster cluster) => atomic(() => Cluster.Swap(_ => cluster));
}