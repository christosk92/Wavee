using Google.Protobuf;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Spotify.Cache.Domain.Chunks;
using Wavee.Spotify.Cache.Domain.Tracks;
using Wavee.Spotify.Cache.Repositories;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Cache;

public static class SpotifyCache<R> where R : struct, HasTrackRepo<R>, HasFileRepo<R>
{
    public static Aff<R, Option<Track>> GetTrack(string id) =>
        from track in TrackRepo<R>.FindOne(t => t.Id == id)
        select track;

    public static Aff<R, Track> CacheTrack(Track item, string id) =>
        from track in TrackRepo<R>.FindOne(t => t.Id == id)
        from result in track.Match(
            Some: c => SuccessAff(c),
            None: () => ForceCacheTrack(item, id)
        )
        select result;

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

    private static Aff<R, Track> ForceCacheTrack(Track item, string id) =>
        from now in Now
        let model = new NewTrack(id, item, now)
        from track in TrackRepo<R>.Create(model)
        select item;

    private static Eff<R, DateTimeOffset> Now =>
        from now in SuccessEff(DateTimeOffset.Now)
        select now;
}