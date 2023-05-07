using Google.Protobuf;
using LanguageExt.Effects.Traits;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Playback.Cdn;
using Wavee.Spotify.Sys.SpApi;

namespace Wavee.Spotify.Sys.Playback.Cdn;

public readonly record struct CdnUrl(ByteString FileId, Seq<MaybeExpiringUrl> Urls);

internal static class CdnUrlFunctions<RT> where RT : struct, HasHttp<RT>
{
    public static async Task<CdnUrl> ResolveAudio(SpotifyConnectionInfo connectionInfo, CdnUrl cdnUrl,
        RT runtime,
        CancellationToken ct)
    {
        var fileId = cdnUrl.FileId;

        var msgMaybe = await SpApiRuntime<RT>.GetAudioStorage(connectionInfo, fileId,
                connectionInfo.Welcome.ValueUnsafe().CanonicalUsername, ct)
            .Run(runtime);
        var msg = msgMaybe.ThrowIfFail();
        var urls = MaybeExpiringUrl.From(msg);

        var newCdnUrl = new CdnUrl(fileId, urls);
        //S_Log.Instance.LogInfo($"Resolved CDN storage: {newCdnUrl}");
        return newCdnUrl;
    }

    public static string GetUrl(CdnUrl cdnUrl)
    {
        if (cdnUrl.Urls is not
            {
                Length: > 0
            })
            throw new CdnException(CdnUrlError.Unresolved);

        var now = DateTimeOffset.UtcNow;
        var url = cdnUrl.Urls
            .Where(u => u.ExpiresAt is null || u.ExpiresAt > now)
            .OrderBy(u => u.ExpiresAt)
            .FirstOrDefault().Url;

        if (string.IsNullOrEmpty(url))
            return cdnUrl.Urls[0].Url;
        return url;
    }
}