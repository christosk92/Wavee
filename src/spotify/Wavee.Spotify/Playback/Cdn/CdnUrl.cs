using System.Text.RegularExpressions;
using Eum.Spotify.storage;
using Google.Protobuf;
using Wavee.Core.Exceptions;

namespace Wavee.Spotify.Playback.Cdn;

internal sealed class CdnUrl
{
    private readonly List<MaybeExpiringUrl> _urls;

    public CdnUrl(ByteString fileId)
    {
        _urls = new List<MaybeExpiringUrl>();
        FileId = fileId;
    }

    public ByteString FileId { get; }

    public async Task ResolveAudio(ISpotifyClient client)
    {
        var response = await client.Cdn.GetAudioStorageAsync(FileId);
        var urls = MaybeExpiringUrl.TryFrom(response);
        _urls.AddRange(urls);
    }

    public bool TryGetUrl(out string? o)
    {
        o = _urls
            .Where(url => url.Expiry == null || url.Expiry > DateTimeOffset.Now)
            .OrderBy(url => url.Expiry)
            .Select(url => url.Url)
            .FirstOrDefault();

        return o != null;
    }
}

internal readonly record struct MaybeExpiringUrl(string Url, DateTimeOffset? Expiry)
{
    private const long CDN_URL_EXPIRY_MARGIN = 10000;

    public static IReadOnlyCollection<MaybeExpiringUrl> TryFrom(StorageResolveResponse response)
    {
        if (response.Result is not StorageResolveResponse.Types.Result.Cdn)
            throw new CannotPlayException(CannotPlayException.Reason.CdnError);

        var isExpiring = !response.Fileid.IsEmpty;
        var result = new List<MaybeExpiringUrl>();
        foreach (var cdnUrl in response.Cdnurl)
        {
            Uri uri;
            DateTime? expiry = null;

            try
            {
                uri = new Uri(cdnUrl);
            }
            catch (UriFormatException)
            {
                Console.WriteLine($"Invalid URL format: {cdnUrl}");
                continue;
            }

            if (isExpiring)
            {
                // var expiryStr = uri.Query.TrimStart('?')
                //     .Split('&')
                //     .Select(part => part.Split('='))
                //     .Where(part => part.Length == 2 && (part[0] == "__token__" || part[0] == "Expires"))
                //     .Select(part => part[1])
                //     .FirstOrDefault();
                //__token__=exp= OR Expires=
                var regex = new Regex(@"(?:__token__=exp=|Expires=)(\d+)");
                var expiryStrMatch = regex.Match(uri.Query);
                var expiryStr = expiryStrMatch.Success ? expiryStrMatch.Groups[1].Value : null;
                if (expiryStr != null)
                {
                    if (expiryStr.Contains("exp="))
                    {
                        expiryStr = expiryStr.Split(new[] { "exp=" }, StringSplitOptions.None).Last().Split('~')
                            .First();
                    }
                    else if (!expiryStr.All(char.IsDigit))
                    {
                        expiryStr = expiryStr.Split('_').First();
                    }

                    if (long.TryParse(expiryStr, out long expiryUnix))
                    {
                        var expiryDate = DateTimeOffset.FromUnixTimeSeconds(expiryUnix).DateTime;
                        expiry = expiryDate.Subtract(TimeSpan.FromMilliseconds(CDN_URL_EXPIRY_MARGIN));
                    }
                    else
                    {
                        Console.WriteLine($"Cannot parse CDN URL expiry timestamp '{expiryStr}' from '{cdnUrl}'");
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown CDN URL format: {cdnUrl}");
                }
            }

            result.Add(new MaybeExpiringUrl(cdnUrl, expiry));
        }

        return result;
    }
}