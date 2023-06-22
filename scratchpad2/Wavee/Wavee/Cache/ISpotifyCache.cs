using LanguageExt;
using Spotify.Metadata;
using Wavee.Id;

namespace Wavee.Cache;

public interface ISpotifyCache
{
    Option<Track> GetTrack(SpotifyId id);
    Unit SetTrack(SpotifyId id, Track track);
    Option<FileStream> File(AudioFile format);
}