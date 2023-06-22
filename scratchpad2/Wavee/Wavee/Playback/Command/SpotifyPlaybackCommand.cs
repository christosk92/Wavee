using LanguageExt;
using Wavee.Id;
using Wavee.Remote;

namespace Wavee.Playback.Command;

internal interface ISpotifyPlaybackCommand
{
    public static ISpotifyPlaybackCommand Play(SpotifyRemoteState spotifyRemoteState) =>
        SpotifyPlayCommand.From(spotifyRemoteState);
}

internal record SpotifyPlayCommand(
    Option<SpotifyId> TrackId,
    Option<string> TrackUid,
    Option<string> ContextUri,
    Option<int> IndexInContext
) : ISpotifyPlaybackCommand
{
    public static SpotifyPlayCommand From(SpotifyRemoteState spotifyRemoteState)
    {
        return new SpotifyPlayCommand(
            TrackId: spotifyRemoteState.TrackId,
            TrackUid: spotifyRemoteState.TrackUid,
            ContextUri: spotifyRemoteState.ContextUri,
            IndexInContext: spotifyRemoteState.IndexInContext
        );
    }
}