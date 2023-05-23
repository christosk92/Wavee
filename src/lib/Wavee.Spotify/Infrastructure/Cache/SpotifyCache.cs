using System.Buffers.Text;
using System.ComponentModel;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using SQLite;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify.Infrastructure.Cache;

public readonly struct SpotifyCache
{
    private static bool _initialized = false;
    private readonly Option<string> _storagePath;
    private readonly Option<string> _dbPath;

    public SpotifyCache(SpotifyCacheConfig cacheConfig)
    {
        _storagePath = cacheConfig.CachePath.Map(x => Path.Combine(x, "Storage"));
        _dbPath = cacheConfig.CachePath.Map(x => Path.Combine(x, "cache.db"));
        if (_dbPath.IsSome && !_initialized)
        {
            using var db = new SQLiteConnection(_dbPath.ValueUnsafe());
            try
            {
                db.CreateTable<CachedTrack>();
            }
            catch (SQLiteException)
            {
            }

            try
            {
                db.CreateTable<CachedEpisode>();
            }
            catch (SQLiteException)
            {
            }

            try
            {
                db.CreateTable<CachedAlbum>();
            }
            catch (SQLiteException)
            {

            }
        }

        _initialized = true;
    }

    #region Metadata

    public Unit Save(TrackOrEpisode metadata)
    {
        if (_dbPath.IsNone)
            return default;

        using var db = new SQLiteConnection(_dbPath.ValueUnsafe());
        if (metadata.Value.IsRight)
        {
            var track = metadata.Value.Match(Left: _ => throw new InvalidOperationException(), Right: x => x);
            db.InsertOrReplace(new CachedTrack
            {
                Id = metadata.Id.ToBase62(),
                Title = track.Name,
                RestData = track.ToByteArray()
            });
        }
        else if (metadata.Value.IsLeft)
        {
            var episode = metadata.Value.Match(Right: _ => throw new InvalidOperationException(), Left: x => x);
            db.InsertOrReplace(new CachedEpisode()
            {
                Id = metadata.Id.ToBase62(),
                Title = episode.Name,
                RestData = episode.ToByteArray()
            });
        }

        return default;
    }

    public Unit SaveRawEntity(AudioId Id, string title,
        ReadOnlyMemory<byte> data,
        DateTimeOffset expiration)
    {
        if (_dbPath.IsNone)
            return default;

        using var db = new SQLiteConnection(_dbPath.ValueUnsafe());

        switch (Id.Type)
        {
            case AudioItemType.Track:
                db.InsertOrReplace(new CachedTrack
                {
                    Id = Id.ToBase62(),
                    Title = title,
                    RestData = data.ToArray()
                });
                break;
            case AudioItemType.PodcastEpisode:
                db.InsertOrReplace(new CachedEpisode
                {
                    Id = Id.ToBase62(),
                    Title = title,
                    RestData = data.ToArray()
                });
                break;
            case AudioItemType.Album:
                db.InsertOrReplace(new CachedAlbum
                {
                    Id = Id.ToBase62(),
                    Title = title,
                    RestData = data.ToArray(),
                    InsertedAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(5)),
                    AbsoluteCacheExpiration = expiration
                });
                break;
        }

        return default;
    }

    public Option<TrackOrEpisode> Get(AudioId id)
    {
        if (_dbPath.IsNone)
            return None;

        using var db = new SQLiteConnection(_dbPath.ValueUnsafe());
        return id.Type switch
        {
            AudioItemType.Track => db.Table<CachedTrack>().SingleOrDefault(x => x.Id == id.ToBase62())
                is CachedTrack track
                ? new TrackOrEpisode(Track.Parser.ParseFrom(track.RestData))
                : None,
            AudioItemType.PodcastEpisode => db.Table<CachedEpisode>().SingleOrDefault(x => x.Id == id.ToBase62())
                is CachedEpisode episode
                ? new TrackOrEpisode(Episode.Parser.ParseFrom(episode.RestData))
                : None,
        };
    }

    public Option<ReadOnlyMemory<byte>> GetRawEntity(AudioId id)
    {
        if (_dbPath.IsNone)
            return None;

        using var db = new SQLiteConnection(_dbPath.ValueUnsafe());
        return id.Type switch
        {
            AudioItemType.Track => db.Table<CachedTrack>().SingleOrDefault(x => x.Id == id.ToBase62())
                is CachedTrack track
                ? (ReadOnlyMemory<byte>)track.RestData
                : None,
            AudioItemType.PodcastEpisode => db.Table<CachedEpisode>().SingleOrDefault(x => x.Id == id.ToBase62())
                is CachedEpisode episode
                ? (ReadOnlyMemory<byte>)episode.RestData
                : None,
            AudioItemType.Album => db.Table<CachedAlbum>().SingleOrDefault(x => x.Id == id.ToBase62())
                is CachedAlbum album && album.AbsoluteCacheExpiration > DateTimeOffset.UtcNow
                ? (ReadOnlyMemory<byte>)album.RestData
                : None,
        };
    }

    #endregion

    #region AudioFiles

    public Option<Stream> OpenEncryptedAudioFile(string trackId, AudioFile.Types.Format format)
    {
        if (_storagePath.IsNone)
            return None;

        var path = Path.Combine(_storagePath.ValueUnsafe(), ((int)format).ToString(), trackId);
        if (!File.Exists(path))
            return None;

        return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public Unit SaveEncryptedFile(string trackId,
        AudioFile.Types.Format format,
        byte[] bytes)
    {
        if (_storagePath.IsNone)
            return default;

        var path = Path.Combine(_storagePath.ValueUnsafe(), ((int)format).ToString(), trackId);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);
        return default;
    }

    #endregion

    private class CachedAlbum
    {
        [PrimaryKey] public string Id { get; init; }
        public string Title { get; init; }
        public byte[] RestData { get; init; }
        public DateTimeOffset InsertedAt { get; init; }
        public DateTimeOffset AbsoluteCacheExpiration { get; init; }
    }
    private class CachedTrack
    {
        [PrimaryKey] public string Id { get; init; }
        public string Title { get; init; }
        public byte[] RestData { get; init; }
    }

    private class CachedEpisode
    {
        [PrimaryKey] public string Id { get; init; }
        public string Title { get; init; }
        public byte[] RestData { get; init; }
    }
}