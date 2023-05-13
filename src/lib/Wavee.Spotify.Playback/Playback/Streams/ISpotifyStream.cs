using Spotify.Metadata;
using Wavee.Player;
using Wavee.Spotify.Clients.Mercury.Metadata;

namespace Wavee.Spotify.Playback.Playback.Streams;

public interface ISpotifyStream : IAudioStream
{
    AudioFile ChosenFile { get; }
    TrackOrEpisode Metadata { get; }
}