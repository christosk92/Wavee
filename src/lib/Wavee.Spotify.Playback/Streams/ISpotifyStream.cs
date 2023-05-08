using Eum.Spotify.connectstate;
using Eum.Spotify.context;
using Wavee.Player;
using Wavee.Spotify.Playback.Sys;

namespace Wavee.Spotify.Playback.Streams;

public interface ISpotifyStream : IAudioStream
{
    ContextTrack Track { get; }
    TrackOrEpisode Metadata { get; }
}