using Google.Protobuf;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;

namespace Wavee.Spotify.Infrastructure.Cache;

public interface ISpotifyCache
{
    Option<Stream> AudioFile(AudioFile file);
    Unit SaveAudioFile(AudioFile file, byte[] data);
    Option<TrackOrEpisode> Get(AudioId audioId);
    Unit Save(TrackOrEpisode fetchedTrack);
    bool[] CheckExists(Seq<AudioId> request);
    Unit SaveBulk(Seq<TrackOrEpisode> result);
    Option<ReadOnlyMemory<byte>> GetRawEntity(string id);
    Unit SaveRawEntity(string Id, string title,
        byte[] data,
        DateTimeOffset expiration);

    Option<SpotifyColors> GetColorFor(string imageUrl);
    Unit SaveColorFor(string imageUrl, SpotifyColors response);
    Task<List<TrackOrEpisode>> GetTracksOriginalSort(Seq<AudioId> ids, string filterString);
}