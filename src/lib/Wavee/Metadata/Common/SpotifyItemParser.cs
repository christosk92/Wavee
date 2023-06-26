using LanguageExt;
using Serilog;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wavee.Id;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Home;

namespace Wavee.Metadata.Common;

internal static class SpotifyItemParser
{
    public static Option<ISpotifyHomeItem> ParseFrom(JsonElement item)
    {
        try
        {
            if (item.GetProperty("__typename").GetString() is "NotFound" or "RestrictedContent")
            {
                Log.Warning("Item not found");
                return Option<ISpotifyHomeItem>.None;
            }
            var uri = item.GetProperty("uri").GetString();
            var id = SpotifyId.FromUri(uri.AsSpan());
          

            switch (id.Type)
            {
                case AudioItemType.Playlist:
                {
                    var name = item.GetProperty("name").GetString()!;
                    var images = ParseImages(item.GetProperty("images").GetProperty("items")
                        .EnumerateArray()
                        .First().GetProperty("sources"));
                    var description = item.TryGetProperty("description", out var desc) &&
                                      desc.ValueKind is JsonValueKind.String
                        ? desc.GetString()
                        : Option<string>.None;
                    var ownerName = item.GetProperty("ownerV2").GetProperty("data").GetProperty("name")
                        .GetString()!;

                    return new SpotifyPlaylistHomeItem
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        Images = images,
                        OwnerName = ownerName
                    };
                    break;
                }
                case AudioItemType.Album:
                {
                    var name = item.GetProperty("name").GetString()!;
                    var images = ParseImages(item.GetProperty("coverArt").GetProperty("sources"));
                    var artists = item.GetProperty("artists").GetProperty("items").EnumerateArray().Select(
                        x =>
                            new TrackArtist
                            {
                                Id = SpotifyId.FromUri(x.GetProperty("uri").GetString()!.AsSpan()),
                                Name = x.GetProperty("profile").GetProperty("name").GetString()!
                            }).ToArray();

                    return new SpotifyAlbumHomeItem
                    {
                        Id = id,
                        Name = name,
                        Artists = artists,
                        Images = images
                    };
                    break;
                }
                case AudioItemType.Artist:
                {
                    var name = item.GetProperty("profile").GetProperty("name").GetString()!;
                    var images = ParseImages(item.GetProperty("visuals").GetProperty("avatarImage")
                        .GetProperty("sources"));

                    return new SpotifyArtistHomeItem
                    {
                        Id = id,
                        Name = name,
                        Images = images
                    };
                    break;
                }
            }

            return Option<ISpotifyHomeItem>.None;
        }
        catch (KeyNotFoundException k)
        {
            //"   at System.Text.Json.JsonElement.GetProperty(String propertyName)\r\n   at Wavee.Metadata.Common.SpotifyItemParser.ParseFrom(JsonElement item) in C:\\Users\\chris-pc\\Desktop\\Wavee\\Wavee\\src\\lib\\Wavee\\Metadata\\Common\\SpotifyItemParser.cs:line 17"
            //"   at System.Text.Json.JsonElement.GetProperty(String propertyName)\r\n   at Wavee.Metadata.Common.SpotifyItemParser.ParseFrom(JsonElement item) in C:\\Users\\chris-pc\\Desktop\\Wavee\\Wavee\\src\\lib\\Wavee\\Metadata\\Common\\SpotifyItemParser.cs:line 17"
            Log.Warning("Could not find key: {key}", k);
            return Option<ISpotifyHomeItem>.None;
        }
    }

    static CoverImage[] ParseImages(JsonElement sources)
    {
        var output = new CoverImage[sources.GetArrayLength()];
        using var sourcesArr = sources.EnumerateArray();
        int i = 0;
        while (sourcesArr.MoveNext())
        {
            var source = sourcesArr.Current;
            var url = source.GetProperty("url").GetString()!;
            var potentialWidth = source.TryGetProperty("width", out var wd)
                                 && wd.ValueKind is JsonValueKind.Number
                ? wd.GetUInt16()
                : Option<ushort>.None;
            var potentialHeight = source.TryGetProperty("height", out var ht)
                                  && ht.ValueKind is JsonValueKind.Number
                ? ht.GetUInt16()
                : Option<ushort>.None;
            output[i] = new CoverImage
            {
                Url = url,
                Width = potentialWidth,
                Height = potentialHeight
            };
            i++;
        }

        return output;
    }

}