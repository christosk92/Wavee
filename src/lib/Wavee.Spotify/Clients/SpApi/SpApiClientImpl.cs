using System.Net.Http.Headers;
using System.Text;
using Eum.Spotify.storage;
using Google.Protobuf;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Contracts.Mercury;
using Wavee.Spotify.Contracts.SpApi;
using Wavee.Spotify.Infrastructure.ApResolver;

namespace Wavee.Spotify.Clients.SpApi;

internal readonly struct SpApiClientImpl<RT> : ISpApi where RT : struct, HasHttp<RT>
{
    private static Option<string> _apUrl = Option<string>.None;

    private readonly IMercuryClient _mercury;
    private readonly RT _runtime;
    private readonly string _userId;

    public SpApiClientImpl(IMercuryClient mercury, RT runtime, string userId)
    {
        _mercury = mercury;
        _runtime = runtime;
        _userId = userId;
    }

    public async ValueTask<StorageResolveResponse> GetAudioStorage(ByteString fileId, CancellationToken ct = default)
    {
        var affResult = await GetAudioStorageInternal(fileId, _mercury, _userId, ct).Run(_runtime);
        return affResult.Match(
            Succ: x => x,
            Fail: ex => throw ex
        );
    }

    private static Aff<RT, StorageResolveResponse> GetAudioStorageInternal(ByteString fileId,
        IMercuryClient mercuryClient,
        string userId,
        CancellationToken ct = default) =>
        from baseUrl in _apUrl.Match(
            Some: x => SuccessAff(x),
            None: () =>
            {
                var map1 = AP<RT>.FetchSpClient()
                    .Map(x => $"https://{x.Host}:{x.Port}")
                    .Map(x =>
                    {
                        _apUrl = x;
                        return x;
                    });
                return map1;
            })
        from bearer in mercuryClient.FetchBearer(ct).ToAff()
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