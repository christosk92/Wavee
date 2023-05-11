using Spotify.Metadata;
using Wavee.Spotify.Clients.Mercury.Metadata;

namespace Wavee.Spotify.Clients.Playback.Streams;

public interface ISpotifyStream
{
    AudioFile ChosenFile { get; }
    TrackOrEpisode Metadata { get; }
    Stream AsStream();
}