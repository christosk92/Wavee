using Mediator;
using Wavee.Spotify.Application.AudioKeys.Queries;
using Wavee.Spotify.Infrastructure.LegacyAuth;

namespace Wavee.Spotify.Application.AudioKeys.QueryHandlers;

public sealed class SpotifyGetAudioKeyQueryHandler : IQueryHandler<SpotifyGetAudioKeyQuery, byte[]>
{
    private readonly HttpClient _httpClient;
    private readonly IMediator _mediator;
    private readonly SpotifyTcpHolder _spotifyTcpHolder;

    public SpotifyGetAudioKeyQueryHandler(IHttpClientFactory factory, IMediator mediator,
        SpotifyTcpHolder spotifyTcpHolder)
    {
        _mediator = mediator;
        _spotifyTcpHolder = spotifyTcpHolder;
        _httpClient = factory.CreateClient(Constants.SpotifyRemoteStateHttpClietn);
    }

    public async ValueTask<byte[]> Handle(SpotifyGetAudioKeyQuery query, CancellationToken cancellationToken)
    {
        var key = await _spotifyTcpHolder.RequestAudioKey(
            itemId: query.ItemId,
            fileId: query.FileId,
            cancellationToken: cancellationToken
        );
        return key;


        // var request = new PlayPlayLicenseRequest
        // {
        //     Version = 3,
        //     CacheId = ByteString.Empty,
        //     Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        //     Interactivity = Interactivity.Interactive,
        //     ContentType = ContentType.AudioTrack,
        //     Token = ByteString.FromBase64("ASfVFE7bIFb8RMGy5oPPiQ==") //TODO: Host this on CDN
        //     // Token = query.FileId
        // };
        //
        // const string url = "https://spclient.com/playplay/v1/key/{0}";
        // var finalUrl = string.Format(url, ToBase16(query.FileId.ToByteArray()).ToLowerInvariant());
        // using var requestMessage = new HttpRequestMessage(HttpMethod.Post, finalUrl);
        // requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/protobuf"));
        // //Content-Type: application/x-www-form-urlencoded
        // using var byteArrayContent = new ByteArrayContent(request.ToByteArray());
        // byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        // requestMessage.Content = byteArrayContent;
        // using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        // response.EnsureSuccessStatusCode();
        // await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        // var result = PlayPlayLicenseResponse.Parser.ParseFrom(stream);
        //
        //
        //
        // return result.ObfuscatedKey.ToByteArray();
    }

    public static string ToBase16(ReadOnlySpan<byte> bytes) => BytesToHex(bytes, 0, bytes.Length, false, -1);

    private static string BytesToHex(ReadOnlySpan<byte> bytes, int offset, int length, bool trim, int minLength)
    {
        int newOffset = 0;
        bool trimming = trim;
        char[] hexChars = new char[length * 2];
        for (int j = offset; j < length; j++)
        {
            int v = bytes[j] & 0xFF;
            if (trimming)
            {
                if (v == 0)
                {
                    newOffset = j + 1;

                    if (minLength != -1 && length - newOffset == minLength)
                        trimming = false;

                    continue;
                }
                else
                {
                    trimming = false;
                }
            }

            hexChars[j * 2] = hexArray[(uint)v >> 4];
            hexChars[j * 2 + 1] = hexArray[v & 0x0F];
        }

        return new string(hexChars, newOffset * 2, hexChars.Length - newOffset * 2);
    }

    private static readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();
}