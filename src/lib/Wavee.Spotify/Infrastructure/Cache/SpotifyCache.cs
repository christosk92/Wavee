using System.Buffers.Text;
using System.ComponentModel;
using Eum.Spotify.playlist4;
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
    public static bool Initialized = false;
    private readonly Option<string> _storagePath;
    private readonly Option<string> _dbPath;

    public SpotifyCache(SpotifyCacheConfig cacheConfig, string locale)
    {
        _storagePath = cacheConfig.AudioCachePath;
        _dbPath = cacheConfig.CachePath.Map(x => Path.Combine(x, $"cache_{locale}.db"));
        if (_dbPath.IsSome && !Initialized)
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

            try
            {
                db.CreateTable<CachedPlaylist>();
            }
            catch (SQLiteException)
            {

            }
        }

        if (_storagePath.IsSome && !Initialized)
        {
            Directory.CreateDirectory(_storagePath.ValueUnsafe());
        }
        Initialized = true;
    }

    #region Metadata

    public Seq<Option<TrackOrEpisode>> GetBulk(Seq<AudioId> request)
    {
        if (request.Length == 0) return LanguageExt.Seq<Option<TrackOrEpisode>>.Empty;
        if (_dbPath.IsNone)
            return new Seq<Option<TrackOrEpisode>>(Enumerable.Repeat(Option<TrackOrEpisode>.None, request.Count));
        using var db = new SQLiteConnection(_dbPath.ValueUnsafe());
        // var result = new List<Option<TrackOrEpisode>>(request.Count);

        var base62IdsMap = request
            .ToDictionary(c => c.ToBase62(), c => c.Type)
            .ToHashMap();
        var base62Ids = base62IdsMap.Keys;
        var tracks = db.Table<CachedTrack>()
            .Where(x => base62Ids.Contains(x.Id))
            .ToArray();
        var episodes = db.Table<CachedEpisode>()
            .Where(x => base62Ids.Contains(x.Id))
            .ToArray();

        var result = base62IdsMap.Map(x =>
            {
                var trackOrEpisode = x.Value switch
                {
                    AudioItemType.PodcastEpisode => episodes.Find(y => y.Id == x.Key)
                        .Map(y => new TrackOrEpisode(Episode.Parser.ParseFrom(y.RestData))),
                    AudioItemType.Track => tracks.Find(y => y.Id == x.Key)
                        .Map(y => new TrackOrEpisode(Track.Parser.ParseFrom(y.RestData)))
                };
                return trackOrEpisode;
            });
        return result.ToSeq();
    }

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
    public Unit SaveBulk(Seq<TrackOrEpisode> result)
    {
        if (_dbPath.IsNone)
            return default;
        if (result.IsEmpty) return default;
        using var db = new SQLiteConnection(_dbPath.ValueUnsafe());
        var tracks = result.Where(x => x.Value.IsRight).Select(x => new CachedTrack
        {
            Id = x.Id.ToBase62(),
            Title = x.Value.Match(Left: _ => throw new InvalidOperationException(), Right: x => x.Name),
            RestData = x.Value.Match(Left: _ => throw new InvalidOperationException(), Right: x => x.ToByteArray())
        });

        var episodes = result.Where(x => x.Value.IsLeft).Select(x => new CachedEpisode
        {
            Id = x.Id.ToBase62(),
            Title = x.Value.Match(Right: _ => throw new InvalidOperationException(), Left: x => x.Name),
            RestData = x.Value.Match(Right: _ => throw new InvalidOperationException(), Left: x => x.ToByteArray())
        });

        db.InsertAll(tracks);
        db.InsertAll(episodes);
        return unit;
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

    #region Playlists

    public Unit SavePlaylist(AudioId id, SelectedListContent content)
    {
        if (_dbPath.IsNone)
            return default;

        using var db = new SQLiteConnection(_dbPath.ValueUnsafe());

        db.InsertOrReplace(new CachedPlaylist
        {
            Id = id.ToString(),
            Revision = content.Revision.ToBase64(),
            Title = content.Attributes.HasName ? content.Attributes.Name
                : "Playlist",
            RestData = content.ToByteArray()
        });

        return default;
    }

    public Option<SelectedListContent> GetPlaylist(AudioId id)
    {
        if (_dbPath.IsNone)
            return None;

        using var db = new SQLiteConnection(_dbPath.ValueUnsafe());

        return db.Table<CachedPlaylist>().SingleOrDefault(x => x.Id == id.ToString())
            is CachedPlaylist playlist
            ? SelectedListContent.Parser.ParseFrom(playlist.RestData)
            : None;
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

    private class CachedPlaylist
    {
        [PrimaryKey] public string Id { get; init; }
        public string Revision { get; init; }
        public string Title { get; init; }
        public byte[] RestData { get; init; }
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