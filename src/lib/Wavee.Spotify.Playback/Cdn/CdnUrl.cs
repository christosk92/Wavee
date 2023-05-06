using Google.Protobuf;
using Wavee.Spotify.Contracts;

namespace Wavee.Spotify.Playback.Cdn;

public readonly record struct CdnUrl(ByteString FileId, Seq<MaybeExpiringUrl> Urls);

internal static class CdnUrlFunctions
{
    public static async Task<CdnUrl> ResolveAudio(ISpotifyClient waveeClient, CdnUrl cdnUrl, CancellationToken ct)
    {
        var fileId = cdnUrl.FileId;

        var msg = await waveeClient.InternalApi.GetAudioStorage(fileId, ct);
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