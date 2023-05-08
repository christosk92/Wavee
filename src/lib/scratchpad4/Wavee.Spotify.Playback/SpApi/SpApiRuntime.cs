using System.Net.Http.Headers;
using System.Text;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt.Effects.Traits;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys;
using Wavee.Spotify.Sys.Tokens;

namespace Wavee.Spotify.Playback.SpApi;

internal static class SpApiRuntime<RT> where RT : struct, HasCancel<RT>, HasHttp<RT>
{
    //GetAudioStorage
    public static Aff<RT, StorageResolveResponse> GetAudioStorage(
        SpotifyConnectionInfo connectionInfo,
        ByteString fileId,
        CancellationToken ct = default) =>
        from baseUrl in AP<RT>.FetchSpClient()
            .Map(x=> $"https://{x.Host}:{x.Port}")
        from bearer in connectionInfo.FetchAccessToken().ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        let url = $"{baseUrl}/storage-resolve/files/audio/interactive/{ToBase16(fileId.Span)}"
        from response in Http<RT>.Get(url, bearer, Option<HashMap<string, string>>.None, ct)
            .MapAsync(async x =>
            {
                x.EnsureSuccessStatusCode();
                await using var data = await x.Content.ReadAsStreamAsync(ct);
                return StorageResolveResponse.Parser.ParseFrom(data);
            })
        select response;

    public static string ToBase16(ReadOnlySpan<byte> fileIdSpan)
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