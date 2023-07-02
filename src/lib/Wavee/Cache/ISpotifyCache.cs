using Eum.Spotify.playlist4;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Sqlite.Entities;
using Wavee.Sqlite.Repository;

namespace Wavee.Cache;

public interface ISpotifyCache
{
    Task<Option<Track>> GetTrack(SpotifyId id);
    Task<Unit> SetTrack(SpotifyId id, Track track);
    Option<FileStream> File(AudioFile format);
    Task<Option<ReadOnlyMemory<byte>>> GetRawEntity(string itemId);
    Task<Unit> SaveRawEntity(string itemId, byte[] data, DateTimeOffset expiration);
    Task<Option<CachedPlaylist>> TryGetPlaylist(SpotifyId spotifyId, CancellationToken ct = default);
    Task SavePlaylist(SpotifyId spotifyId, SelectedListContent playlistResult, CancellationToken ct = default);
    Task<Dictionary<string, Option<CachedTrack>>> GetTracksFromCache(string[] ids);
    Task<Dictionary<string, Option<CachedEpisode>>> GetEpisodesFromCache(string[] ids);
    Task<Unit> AddTracksToCache(Dictionary<string, TrackWithExpiration> newTracks);
    Task<Unit> AddEpisodesToCache(Dictionary<string, EpisodeWithExpiration> newTracks);
}