using System.Text.RegularExpressions;
using Eum.Spotify.storage;
using LanguageExt;

namespace Wavee.Spotify.Infrastructure.Playback.Cdn;

public readonly record struct MaybeExpiringUrl(string Url, DateTimeOffset? ExpiresAt)
{
    public static Seq<MaybeExpiringUrl> From(StorageResolveResponse resp)
    {
        if (resp.Result != StorageResolveResponse.Types.Result.Cdn)
            throw new CdnException(CdnUrlError.Storage);

        var isExpiring = !resp.Fileid.IsEmpty;
        return resp.Cdnurl
            .Select(cdn_url =>
            {
                var url = new Uri(cdn_url);
                if (isExpiring)
                {
                    var expiry_str = url.Query.Contains("__token__")
                        ? Regex.Match(url.Query, @"exp=([0-9]+)").Groups[1].Value
                        : url.Query.TrimStart('?').Split('_').FirstOrDefault() ?? string.Empty;

                    if (!long.TryParse(expiry_str, out var expiry))
                        throw new Exception($"Unable to parse expiry string {expiry_str}");

                    expiry -= 5 * 60; // seconds

                    return new MaybeExpiringUrl(cdn_url, DateTimeOffset.FromUnixTimeSeconds(expiry));
                }

                return new MaybeExpiringUrl(cdn_url, null);
            })
            .ToSeq();
    }
}