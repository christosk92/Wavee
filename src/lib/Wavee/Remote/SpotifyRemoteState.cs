using Eum.Spotify.connectstate;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Wavee.Id;

namespace Wavee.Remote;

public readonly record struct SpotifyRemoteState(
    Option<SpotifyId> TrackId,
    Option<string> TrackUid,
    Option<string> ContextUri,
    Option<int> IndexInContext)
{
    internal static SpotifyRemoteState? ParseFrom(Option<Cluster> cluster)
    {
        if (cluster.IsNone)
            return null;

        var clusterValue = cluster.IfNoneUnsafe(() => throw new InvalidOperationException());
        var trackUri = clusterValue!.PlayerState?.Track?.Uri;
        var trackId = !string.IsNullOrEmpty(trackUri) ? SpotifyId.FromUri(trackUri.AsSpan()) : Option<SpotifyId>.None;
        var trackUidStr = clusterValue!.PlayerState?.Track?.Uid;
        var trackUid = !string.IsNullOrEmpty(trackUidStr) ? trackUidStr : Option<string>.None;

        var contextUriStr = clusterValue!.PlayerState?.ContextUri;
        var contextUri = !string.IsNullOrEmpty(contextUriStr) ? contextUriStr : Option<string>.None;
        var index = clusterValue!.PlayerState?.Index?.Track is not null ? (int)clusterValue!.PlayerState.Index.Track : Option<int>.None;
        return new SpotifyRemoteState(
            TrackId: trackId,
            TrackUid: trackUid,
            ContextUri: contextUri,
            IndexInContext: index
        );
    }
}