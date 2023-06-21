using Eum.Spotify.connectstate;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Wavee.Id;

namespace Wavee.Remote;

public readonly record struct SpotifyRemoteState(
    Option<SpotifyId> TrackId
)
{
    internal static SpotifyRemoteState? ParseFrom(Option<Cluster> cluster)
    {
        if (cluster.IsNone)
            return null;

        var clusterValue = cluster.IfNoneUnsafe(() => throw new InvalidOperationException());
        var trackUri = clusterValue!.PlayerState?.Track?.Uri;
        var trackId = !string.IsNullOrEmpty(trackUri) ? SpotifyId.FromUri(trackUri) : Option<SpotifyId>.None;
        return new SpotifyRemoteState(
            TrackId: trackId
        );
    }
}