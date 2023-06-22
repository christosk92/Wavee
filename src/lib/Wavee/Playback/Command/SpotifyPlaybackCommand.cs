using LanguageExt;
using Wavee.Id;
using Wavee.Remote;

namespace Wavee.Playback.Command;

internal interface ISpotifyPlaybackCommand
{
   }

internal record SpotifyPlayCommand(
    Option<SpotifyId> TrackId,
    Option<string> TrackUid,
    Option<string> ContextUri,
    Option<int> IndexInContext,
    Ref<Option<TimeSpan>> CrossfadeDuration
) : ISpotifyPlaybackCommand
{
    public static SpotifyPlayCommand From(SpotifyRemoteState spotifyRemoteState, Ref<Option<TimeSpan>> crossfadeDuration)
    {
        return new SpotifyPlayCommand(
            TrackId: spotifyRemoteState.TrackId,
            TrackUid: spotifyRemoteState.TrackUid,
            ContextUri: spotifyRemoteState.ContextUri,
            IndexInContext: spotifyRemoteState.IndexInContext,
            CrossfadeDuration: crossfadeDuration
        );
    }
}