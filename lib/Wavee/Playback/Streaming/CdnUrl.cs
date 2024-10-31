using Eum.Spotify.storage;
using Wavee.Interfaces;
using Wavee.Models.Common;

namespace Wavee.Playback.Streaming;

public class CdnUrlError : Exception
{
    public CdnUrlError(string message) : base(message)
    {
    }
}

public class MaybeExpiringUrl
{
    public string Url { get; set; }
    public DateTime? ExpiryTime { get; set; }

    public MaybeExpiringUrl(string url, DateTime? expiryTime)
    {
        Url = url;
        ExpiryTime = expiryTime;
    }
}

public class MaybeExpiringUrls : List<MaybeExpiringUrl>
{
    public MaybeExpiringUrls() : base()
    {
    }

    public MaybeExpiringUrls(IEnumerable<MaybeExpiringUrl> collection) : base(collection)
    {
    }
}

public class CdnUrl
{
    public FileId FileId { get; private set; }
    private MaybeExpiringUrls Urls { get; set; }

    private static readonly TimeSpan CdnUrlExpiryMargin = TimeSpan.FromMinutes(5);

    public CdnUrl(FileId fileId)
    {
        FileId = fileId;
        Urls = new MaybeExpiringUrls();
    }

    public async Task<CdnUrl> ResolveAudio(ISpotifyApiClient session)
    {
        var msg = await session.GetAudioStorageAsync(FileId, true);
        var urls = TryFromCdnUrlMessage(msg);

        var cdnUrl = new CdnUrl(FileId) { Urls = urls };
        Console.WriteLine($"Resolved CDN storage: {cdnUrl}");

        return cdnUrl;
    }

    public bool TryGetUrl(out string outputUrl)
    {
        if (!Urls.Any())
        {
            throw new CdnUrlError("No URLs resolved");
        }

        var now = DateTime.UtcNow;
        var url = Urls.FirstOrDefault(url => !url.ExpiryTime.HasValue || url.ExpiryTime.Value > now);

        if (url != null)
        {
            outputUrl = url.Url;
            return true;
        }
        
        outputUrl = null;
        return false;
    }

    private static MaybeExpiringUrls TryFromCdnUrlMessage(StorageResolveResponse msg)
    {
        if (msg.Result != StorageResolveResponse.Types.Result.Cdn)
        {
            throw new CdnUrlError("Resolved storage is not for CDN");
        }

        var isExpiring = !msg.Fileid.IsEmpty;

        var result = msg.Cdnurl.Select(cdnUrl =>
        {
            var url = new Uri(cdnUrl);
            DateTime? expiry = null;

            if (isExpiring)
            {
                string expiryStr = null;
                var query = System.Web.HttpUtility.ParseQueryString(url.Query);

                if (query["__token__"] != null)
                {
                    var token = query["__token__"];
                    var expIndex = token.IndexOf("exp=");
                    if (expIndex != -1)
                    {
                        var expEnd = token.IndexOf('~', expIndex);
                        expiryStr = expEnd != -1
                            ? token.Substring(expIndex + 4, expEnd - expIndex - 4)
                            : token.Substring(expIndex + 4);
                    }
                }
                else if (query["Expires"] != null)
                {
                    expiryStr = query["Expires"].Split('~')[0];
                }
                else if (url.Query.StartsWith("?"))
                {
                    expiryStr = url.Query.Substring(1).Split('_')[0];
                }

                if (long.TryParse(expiryStr, out long timestamp))
                {
                    var expiryAt = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                    var withMargin = expiryAt.Subtract(CdnUrlExpiryMargin);
                    expiry = withMargin;
                }
                else if (!string.IsNullOrEmpty(expiryStr))
                {
                    Console.WriteLine($"Cannot parse CDN URL expiry timestamp '{expiryStr}' from '{cdnUrl}'");
                }
                else
                {
                    Console.WriteLine($"Unknown CDN URL format: {cdnUrl}");
                }
            }

            return new MaybeExpiringUrl(cdnUrl, expiry);
        }).ToList();

        return new MaybeExpiringUrls(result);
    }

    public override string ToString()
    {
        return
            $"CdnUrl {{ file_id: {FileId}, urls: {string.Join(", ", Urls.Select(u => $"{u.Url} (Expires: {u.ExpiryTime})"))} }}";
    }
}