using Google.Protobuf;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;

namespace Wavee.Spotify.Infrastructure.Cache;

public interface ISpotifyCache
{
    Option<Stream> AudioFile(AudioFile file);
    Unit SaveAudioFile(AudioFile file, byte[] data);
    Option<TrackOrEpisode> Get(AudioId audioId);
    Unit Save(TrackOrEpisode fetchedTrack);
    Dictionary<AudioId, Option<TrackOrEpisode>> GetBulk(Seq<AudioId> request);
    Unit SaveBulk(Seq<TrackOrEpisode> result);
    Option<ReadOnlyMemory<byte>> GetRawEntity(AudioId id);
    Unit SaveRawEntity(AudioId Id, string title,
        ReadOnlyMemory<byte> data,
        DateTimeOffset expiration);
}