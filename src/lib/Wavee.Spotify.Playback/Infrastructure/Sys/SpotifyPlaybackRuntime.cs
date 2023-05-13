using System.Net.Http.Headers;
using System.Text;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Effects.Traits;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Sys.IO;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Cache;
using Wavee.Spotify.Playback.Infrastructure.Key;
using Wavee.Spotify.Playback.Metadata;
using Wavee.Spotify.Playback.Playback.Cdn;

namespace Wavee.Spotify.Playback.Infrastructure.Sys;

public static class SpotifyPlaybackRuntime<RT> where RT : struct, HasHttp<RT>, HasDatabase<RT>
{
    public static Aff<RT, ISpotifyStream> LoadTrack(
        string spClientUrl,
        TrackOrEpisode metadata,
        Option<string> uid,
        Func<TrackOrEpisode, ITrack> mapper,
        PreferredQualityType preferredQuality,
        Func<ValueTask<string>> getBearer,
        Func<AudioId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, AudioKey>>>
            fetchAudioKeyFunc, CancellationToken ct) =>
        //from file in Eff(() => metadata.FindFile(preferredQuality).ValueUnsafe())
        from file in Eff<RT, AudioFile>((_) =>
        {
            var chosenFile = metadata.FindFile(preferredQuality)
                .Match(
                    Some: x => x,
                    None: () => metadata.FindAlternativeFile(preferredQuality)
                        .Match(Some: x => x,
                            None: () => throw new Exception("No file found"))
                );
            return chosenFile;
        })
        from audioKey in fetchAudioKeyFunc(metadata.Id, file.FileId, ct)
            .Map(e => e.Match(
                Left: x => throw new Exception("Error fetching audio key"),
                Right: x => x))
        from chosenEncryptionStreamAff in SpotifyCache<RT>.GetFile(file.FileId)
            .Map(x => x.Match(
                Some: cachedFile => SpotifyStreams<RT>.OpenEncryptedFileStream(cachedFile, metadata),
                None: () =>
                    from cdnUrl in GetUrl(spClientUrl, file, getBearer, ct)
                    from encryptedStream in SpotifyStreams<RT>.OpenEncryptedStream(file, cdnUrl.Urls.First(), metadata)
                    select encryptedStream
            ))
        let isVorbis = file.Format is AudioFile.Types.Format.OggVorbis96
            or AudioFile.Types.Format.OggVorbis160
            or AudioFile.Types.Format.OggVorbis320
        from encryptedStream in chosenEncryptionStreamAff
        from decryptedStream in SpotifyStreams<RT>.OpenDecryptionStream(encryptedStream, audioKey, isVorbis)
        from subFile in SpotifyStreams<RT>.ExtractFinalStream(decryptedStream.Stream,
            decryptedStream.NormalisationDatas, isVorbis, file,
            metadata,
            uid,mapper)
        select (ISpotifyStream)subFile;

    private static LanguageExt.Aff<RT, CdnUrl> GetUrl(
        string spClientUrl,
        AudioFile file, Func<ValueTask<string>> getBearer,
        CancellationToken ct = default) =>
        from jwt in getBearer().ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from cdnUrl in GetAudioStorage(spClientUrl, jwt, file.FileId, ct)
        let urls = MaybeExpiringUrl.From(cdnUrl)
        let cdnUrlResposne = new CdnUrl(file.FileId, urls)
        select cdnUrlResposne;

    private static LanguageExt.Aff<RT, StorageResolveResponse> GetAudioStorage(
        string spClientUrl,
        AuthenticationHeaderValue header,
        ByteString fileId,
        CancellationToken ct = default)
    {
        var url = $"{spClientUrl}/storage-resolve/files/audio/interactive/{ToBase16(fileId.Span)}";
        return
            from response in Http<RT>.Get(url, header, LanguageExt.Option<LanguageExt.HashMap<string, string>>.None, ct)
                .MapAsync(async x =>
                {
                    x.EnsureSuccessStatusCode();
                    await using var data = await x.Content.ReadAsStreamAsync(ct);
                    return StorageResolveResponse.Parser.ParseFrom(data);
                })
            select response;
    }


    private static string ToBase16(ReadOnlySpan<byte> fileIdSpan)
    {
        Span<byte> buffer = new byte[40];
        var i = 0;
        foreach (var v in fileIdSpan)
        {
            buffer[i] = BASE16_DIGITS[v >> 4];
            buffer[i + 1] = BASE16_DIGITS[v & 0x0f];
            i += 2;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    private static readonly byte[] BASE16_DIGITS = "0123456789abcdef".Select(c => (byte)c).ToArray();
}

public interface ISpotifyStream : IAudioStream
{
    AudioFile ChosenFile { get; }
    Option<string> Uid { get; }
}