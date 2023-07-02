using Eum.Spotify.playlist4;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Sqlite.Entities;

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
}