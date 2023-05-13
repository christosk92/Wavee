using Google.Protobuf;
using LanguageExt;
using LanguageExt.Effects.Database;
using LanguageExt.Effects.Traits;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.Data.Sqlite;
using Spotify.Metadata;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Cache;

public static class SpotifyCache<R> where R : struct,
     HasDatabase<R>
{
    public static DatabaseLive BuildSqliteConnection(string path)
    {
        var uri = $"Data Source={path}";
        var db = new DataConnection(
            new DataOptions()
                .UseSQLite(uri));
        //create table if not exists
        try
        {
            db.CreateTable<SpotifyCachedTrack>();
        }
        catch (SqliteException)
        {
            //ignore
        }
        //EncryptedFiles
        try
        {
            db.CreateTable<FileEntity>();
        }
        catch (SqliteException)
        {
            //ignore
        }

        return new DatabaseLive(db);
    }

    public static Aff<R, Option<Track>> GetTrack(ByteString key) =>
        from b in SuccessEff(key.ToBase64())
        from cache in Database<R>.FindOne<SpotifyCachedTrack>(x => x.Id.Equals(b))
            .Map(x => x.Match(
                Some: r => ToTrack(r),
                None: () => Option<Track>.None)
            )
        select cache;

    public static Aff<R, Unit> Create(Track track, DateTimeOffset CacheUntil) =>
        from now in Now
        from cachedEntry in SuccessEff(ToCachedTrack(track, CacheUntil))
        from _ in Database<R>.Insert<SpotifyCachedTrack, string>(cachedEntry)
        select unit;

    public static Aff<R, Option<EncryptedAudioFile>> GetFile(ByteString audioFileId) =>
        from fileId in SuccessAff(audioFileId.ToBase64())
        from file in FileRepo<R>.FindOne(f => f.Id == fileId)
        select file;
    
    public static Aff<R, EncryptedAudioFile> CacheFile(AudioFile originalFile, ReadOnlyMemory<byte> data) =>
        from fileId in SuccessAff(originalFile.FileId.ToBase64())
        from file in FileRepo<R>.FindOne(f => f.Id == fileId)
        from result in file.Match(
            Some: c => SuccessAff(c),
            None: () => ForceCacheFile(originalFile, data)
        )
        select result;
    private static Aff<R, EncryptedAudioFile> ForceCacheFile(AudioFile f,
        ReadOnlyMemory<byte> data) =>
        from now in Now 
        let model = new NewAudioFile(f, data, now)
        from file in FileRepo<R>.Create(model)
        select file;
    
    private static Eff<DateTimeOffset> Now => SuccessEff(DateTimeOffset.UtcNow);

    private static Track ToTrack(SpotifyCachedTrack spotifyCachedTrack)
    {
        return Track.Parser.ParseFrom(spotifyCachedTrack.TrackData);
    }

    private static SpotifyCachedTrack ToCachedTrack(Track track, DateTimeOffset CacheUntil) =>
        new SpotifyCachedTrack(
            track.Gid.ToBase64(),
            track.Name,
            track.ToByteArray(),
            DateTimeOffset.UtcNow,
            CacheUntil
        );
    
}

[Table("SpotifyTracks")]
public record SpotifyCachedTrack(
    [property: Column(IsPrimaryKey = true)]
    string Id,
    [property: Column] string Name,
    [property: Column] byte[] TrackData,
    [property: Column] DateTimeOffset CreatedAt,
    [property: Column] DateTimeOffset CacheUntil
) : IEntity<string>;