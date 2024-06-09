using System;
using System.Diagnostics;
using System.Text.Json;
using Spotify.Metadata;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.Contracts.Models;
using Wavee.UI.Spotify.Extensions;

namespace Wavee.UI.Spotify.Responses.Parsers;

public static class CommonParsers
{
    public static UrlImage ToUrlImage(this Image image)
    {
        const string url = "https://i.scdn.co/image/";
        var imageId = image.FileId.Span.ToBase16();
        int? height = image.HasHeight ? image.Height : null;
        int? width = image.HasWidth ? image.Width : null;
        return new UrlImage($"{url}{imageId}", width, height);
    }

    public static IHomeItem ParseHomeItem(this JsonElement element)
    {
        var item = element.ParseItem();
        string? color = null;
        string? description = null;
        switch (item)
        {
            case SpotifySimpleArtist:
            {
                var visuals = element.GetProperty("visuals").GetProperty("avatarImage");
                if (visuals.TryGetProperty("extractedColors", out var extractedColors))
                {
                    color = extractedColors.GetProperty("colorDark").GetProperty("hex").GetString();
                }

                break;
            }
            case SimpleAlbum a:
            {
                var coverArt = element.GetProperty("coverArt");
                if (coverArt.TryGetProperty("extractedColors", out var extractedColors))
                {
                    color = extractedColors.GetProperty("colorDark").GetProperty("hex").GetString();
                }
                else
                {
                    color = null;
                }

                description = a.Contributor.Name;
                break;
            }
            case SpotifySimplePlaylist:
            {
                using var imagesRoot = element.GetProperty("images").GetProperty("items").EnumerateArray();
                if (imagesRoot.MoveNext())
                {
                    var imagesItem = imagesRoot.Current;
                    if (imagesItem.TryGetProperty("extractedColors", out var colors))
                    {
                        color = colors
                            .GetProperty("colorDark")
                            .GetProperty("hex")
                            .GetString();
                    }
                }

                break;
            }
        }

        if (item is not null)
        {
            var homeItem = new SpotifyHomeItem(item, color, description);
            return homeItem;
        }

        Debug.WriteLine($"Failed to parse home item of type {item?.GetType().Name}");
        return null;
    }

    public static IItem ParseItem(this JsonElement element)
    {
        var type = element.GetProperty("__typename").GetString();
        var res = type switch
        {
            "Artist" => element.ParseArtist(),
            "Playlist" => element.ParsePlaylist(),
            "Album" => element.ParseAlbum() as IItem,
            "NotFound" => null,
            _ => null
        };

        if (res == null)
        {
            Debug.WriteLine($"Failed to parse item of type {type}");
        }

        return res;
    }

    public static UrlImage[] ParseImages(this JsonElement element)
    {
        var images = element.EnumerateArray();
        Span<UrlImage> res = new UrlImage[5];
        var i = 0;
        foreach (var image in images)
        {
            var heightProperty = image.GetProperty("height");
            int? height = heightProperty.ValueKind == JsonValueKind.Number ? heightProperty.GetInt32() : null;
            var widthProperty = image.GetProperty("width");
            int? width = widthProperty.ValueKind == JsonValueKind.Number ? widthProperty.GetInt32() : null;
            res[i] = new UrlImage(image.GetProperty("url").GetString(), width, height);
            i++;
        }

        return res[..i].ToArray();
    }
}