using Eum.Spotify.connectstate;
using LanguageExt;

namespace Wavee.Spotify.Remote;

public interface ISpotifyRemoteClient
{
    Option<Cluster> Cluster { get; }
    IObservable<Option<Cluster>> ClusterUpdated { get; }
}