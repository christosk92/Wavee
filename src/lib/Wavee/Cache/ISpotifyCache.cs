using LanguageExt;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Metadata.Artist;

namespace Wavee.Cache;

public interface ISpotifyCache
{
    Option<Track> GetTrack(SpotifyId id);
    Unit SetTrack(SpotifyId id, Track track);
    Option<FileStream> File(AudioFile format);
    Option<byte[]> GetRawEntity(string itemId);
    Unit SaveRawEntity(string itemId, byte[] data);
}