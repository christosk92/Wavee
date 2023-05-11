using System.Net.Http.Headers;
using System.Text;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Mercury.Key;
using Wavee.Spotify.Clients.Mercury.Metadata;
using Wavee.Spotify.Clients.Playback.Cdn;
using Wavee.Spotify.Clients.Playback.Streams;
using Wavee.Spotify.Configs;

namespace Wavee.Spotify.Infrastructure.Sys;

internal static class SpotifyPlayback<RT> where RT : struct, HasHttp<RT>
{
    public static Aff<RT, ISpotifyStream> LoadTrack(
        SpotifyId id,
        PreferredQualityType preferredQuality,
        Func<ValueTask<string>> getBearer,
        Func<SpotifyId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, AudioKey>>>
            fetchAudioKeyFunc,
        IMercuryClient mercury, CancellationToken ct) =>
        from metadata in FetchMetadata<RT>(mercury, id, ct)
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
        from cdnUrl in GetUrl(file, getBearer, ct)
        let isVorbis = file.Format is AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.OggVorbis160
            or AudioFile.Types.Format.OggVorbis320
        from encryptedStream in SpotifyStreams<RT>.OpenEncryptedStream(cdnUrl.Urls.First(), metadata)
        from decryptedStream in SpotifyStreams<RT>.OpenDecryptionStream(encryptedStream, audioKey, isVorbis)
        from subFile in SpotifyStreams<RT>.ExtractFinalStream(decryptedStream.Stream,
            decryptedStream.NormalisationDatas, isVorbis, file,
            metadata)
        select (ISpotifyStream)subFile;

    private static Aff<RT, TrackOrEpisode> FetchMetadata<THasHttp>(IMercuryClient mercury, SpotifyId id,
        CancellationToken ct) where THasHttp : struct, HasHttp<THasHttp>
    {
        return id.Type switch
        {
            AudioItemType.Track => mercury.GetTrack(id, ct).ToAff()
                .Map(x => new TrackOrEpisode(Right(x))),
            AudioItemType.Episode => mercury.GetEpisode(id, ct).ToAff()
                .Map(x => new TrackOrEpisode(Left(x))),
            _ => FailAff<RT, TrackOrEpisode>(Error.New("Unsupported type"))
        };
    }

    private static Aff<RT, CdnUrl> GetUrl(AudioFile file, Func<ValueTask<string>> getBearer,
        CancellationToken ct = default) =>
        from jwt in getBearer().ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from cdnUrl in GetAudioStorage(jwt, file.FileId, ct)
        let urls = MaybeExpiringUrl.From(cdnUrl)
        let cdnUrlResposne = new CdnUrl(file.FileId, urls)
        select cdnUrlResposne;

    private static Aff<RT, StorageResolveResponse> GetAudioStorage(
        AuthenticationHeaderValue header,
        ByteString fileId,
        CancellationToken ct = default) =>
        from baseUrl in AP<RT>.FetchSpClient()
            .Map(x => $"https://{x.Host}:{x.Port}")
        let url = $"{baseUrl}/storage-resolve/files/audio/interactive/{ToBase16(fileId.Span)}"
        from response in Http<RT>.Get(url, header, Option<HashMap<string, string>>.None, ct)
            .MapAsync(async x =>
            {
                x.EnsureSuccessStatusCode();
                await using var data = await x.Content.ReadAsStreamAsync(ct);
                return StorageResolveResponse.Parser.ParseFrom(data);
            })
        select response;


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