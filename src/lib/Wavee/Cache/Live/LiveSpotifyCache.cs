using System.Diagnostics;
using System.Text.Json;
using Eum.Spotify.playlist4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using SQLite;
using Wavee.Id;
using Wavee.Sqlite;
using Wavee.Sqlite.Entities;
using Wavee.Sqlite.Repository;

namespace Wavee.Cache.Live;

internal sealed class LiveSpotifyCache : ISpotifyCache
{
    private readonly Func<TracksRepository> _tracksRepository;
    private readonly Func<RawEntityRepository> _rawEntityRepository;
    private readonly Func<PlaylistRepository> _playlistRepository;
    private readonly SpotifyCacheConfig _config;
    private Option<string> DbPath => _config.CacheLocation.Map(x => Path.Combine(x, "spotify.db"));
    private Option<string> FileCachePath => _config.CacheLocation.Map(x => Path.Combine(x, "files"));
    public LiveSpotifyCache(SpotifyCacheConfig Config)
    {
        _config = Config;
        _tracksRepository = () => new TracksRepository(DbPath.IfNone(string.Empty));
        _rawEntityRepository = () => new RawEntityRepository(DbPath.IfNone(string.Empty));
        _playlistRepository = () => new PlaylistRepository(DbPath.IfNone(string.Empty));
    }

    public async Task<Option<Track>> GetTrack(SpotifyId id)
    {
        var result = await _tracksRepository().GetTrack(id.ToString());
        if (result is null) return Option<Track>.None;
        return result;
    }

    public async Task<Unit> SetTrack(SpotifyId id, Track track)
    {
        var result = await _tracksRepository().InsertTrack(id.ToString(), track, DateTimeOffset.MaxValue);
        return result;
    }

    public Option<FileStream> File(AudioFile format)
    {
        return FileCachePath
            .Bind(path =>
            {
                Directory.CreateDirectory(path);
                var file = Path.Combine(path, format.ToString());
                if (!System.IO.File.Exists(file))
                    return Option<FileStream>.None;
                return new FileStream(file, FileMode.Open, FileAccess.Read);
            });
    }

    public async Task<Option<ReadOnlyMemory<byte>>> GetRawEntity(string itemId)
    {
        var result = await _rawEntityRepository().GetEntity(itemId, CancellationToken.None);
        if (result is null) return Option<ReadOnlyMemory<byte>>.None;
        return result.Value;
    }

    public async Task<Unit> SaveRawEntity(string itemId, byte[] data, DateTimeOffset expiration)
    {
        await _rawEntityRepository().SetEntity(itemId, data, expiration, CancellationToken.None);
        return Unit.Default;
    }

    public async Task<Option<CachedPlaylist>> TryGetPlaylist(SpotifyId spotifyId, CancellationToken ct = default)
    {
        var result = await _playlistRepository().GetPlaylist(spotifyId.ToString());
        return result ?? Option<CachedPlaylist>.None;
    }

    public Task SavePlaylist(SpotifyId spotifyId, SelectedListContent playlistResult,
        CancellationToken ct = default)
    {
        var idStr = spotifyId.ToString();
        var playlist = new CachedPlaylist
        {
            Id = idStr,
            Data = playlistResult.ToByteArray(),
            Name = playlistResult.Attributes.Name ?? "Unknown playlist",
            PlaylistTracks = playlistResult.Contents.Items
                .Select(x =>
                {
                    var uid = x.Attributes.HasItemId ? x.Attributes.ItemId.ToBase64() : string.Empty;
                    if (string.IsNullOrEmpty(uid))
                        Debugger.Break();
                    return new CachedPlaylistTrack
                    {
                        Id = x.Uri,
                        Track = null,
                        Uid = uid,
                        MetadataJson = JsonSerializer.Serialize(x.Attributes),
                        PlaylistIdTrackIdCompositeKey = $"{uid}-{idStr}",
                        // CachedPlaylistId = spotifyId.ToString()
                    };
                }).ToList()
        };

        return _playlistRepository().SetPlaylist(playlist);
    }
}