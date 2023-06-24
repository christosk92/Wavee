using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Serilog;
using Spotify.Metadata;
using SQLite;
using Wavee.Cache.Entities;
using Wavee.Id;

namespace Wavee.Cache.Live;

internal sealed class LiveSpotifyCache : ISpotifyCache
{
    private readonly SpotifyCacheConfig _config;
    private Option<string> DbPath => _config.CacheLocation.Map(x => Path.Combine(x, "spotify.db"));
    private Option<string> FileCachePath => _config.CacheLocation.Map(x => Path.Combine(x, "files"));
    public LiveSpotifyCache(SpotifyCacheConfig Config)
    {
        _config = Config;
        if (DbPath.IsSome)
        {
            try
            {
                using var db = new SQLiteConnection(DbPath.ValueUnsafe());
                db.CreateTable<CachedSpotifyTrack>();
            }
            catch (Exception e)
            {
            }
        }
    }

    public Option<Track> GetTrack(SpotifyId id)
    {
        return DbPath
            .Bind(path =>
            {
                using var db = new SQLiteConnection(path);
                var track = db.Find<CachedSpotifyTrack>(id.ToString());
                if (track is null)
                    return Option<Track>.None;
                return Track.Parser.ParseFrom(track.Data);
            });
    }

    public Unit SetTrack(SpotifyId id, Track track)
    {
        return DbPath
            .Map(path =>
            {
                using var db = new SQLiteConnection(path);
                db.InsertOrReplace(new CachedSpotifyTrack
                {
                    Id = id.ToString(),
                    Data = track.ToByteArray()
                });
                return Unit.Default;
            }).IfNone(Unit.Default);
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

    public Option<byte[]> GetRawEntity(string itemId)
    {
        return DbPath
            .Bind(path =>
            {
                using var db = new SQLiteConnection(path);
                var track = db.Find<CachedSpotifyTrack>(itemId);
                if (track is null)
                    return Option<byte[]>.None;
                return track.Data;
            });
    }

    public Unit SaveRawEntity(string itemId, byte[] data)
    {
       return DbPath
            .Map(path =>
            {
                using var db = new SQLiteConnection(path);
                db.InsertOrReplace(new CachedSpotifyTrack
                {
                    Id = itemId,
                    Data = data
                });
                return Unit.Default;
            }).IfNone(Unit.Default);
    }
}