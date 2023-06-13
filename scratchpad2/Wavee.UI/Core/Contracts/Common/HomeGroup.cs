using LanguageExt;
using System.Text.Json;
using Wavee.Core.Ids;

namespace Wavee.UI.Core.Contracts.Common;

public sealed class HomeGroup
{
    public required string Id { get; init; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public IReadOnlyList<CardItem> Items { get; set; }

    public static IReadOnlyList<HomeGroup> ParseFrom(JsonDocument home)
    {
        var groupResults = new List<HomeGroup>();
        if (home.RootElement.TryGetProperty("content", out var ct)
            && ct.TryGetProperty("items", out var items))
        {
            using var itemsArr = items.EnumerateArray();
            foreach (var group in itemsArr)
            {
                var content = group.GetProperty("content");
                using var itemsInGroup = content.GetProperty("items").EnumerateArray();
                var result = new List<CardItem>();
                foreach (var item in itemsInGroup)
                {
                    var type = item.GetProperty("type").GetString();
                    string? image = null; //TODO: Total violation of FP
                    if (item.TryGetProperty("images", out var imgs))
                    {
                        using var images = imgs.EnumerateArray();
                        var artwork = LanguageExt.Seq<Artwork>.Empty;
                        foreach (var jsonImage in images)
                        {
                            var h = jsonImage.TryGetProperty("height", out var height)
                                    && height.ValueKind is JsonValueKind.Number
                                ? height.GetInt32()
                                : Option<int>.None;
                            var w = jsonImage.TryGetProperty("width", out var width)
                                    && width.ValueKind is JsonValueKind.Number
                                ? width.GetInt32()
                                : Option<int>.None;

                            var url = jsonImage.GetProperty("url").GetString();
                            var relativeSize =
                                h.Map(x =>
                                    x switch
                                    {
                                        < 100 => ArtworkSizeType.Small,
                                        < 400 => ArtworkSizeType.Default,
                                        _ => ArtworkSizeType.Large
                                    }).IfNone(ArtworkSizeType.Default);

                            artwork = artwork.Add(new Artwork(url, w, h, relativeSize));
                        }

                        // we may have 3 images (large, medium, small)
                        //or 1 image (large)
                        //get medium if it exists, otherwise get large
                        image = artwork.Find(x => x.Size == ArtworkSizeType.Default)
                            .Match(x => x.Url, () => artwork.Find(x => x.Size == ArtworkSizeType.Default)
                                .Match(x => x.Url,
                                    () => artwork.HeadOrNone().Map(x => x.Url)))
                            .IfNone(string.Empty);
                    }

                    switch (type)
                    {
                        case "playlist":
                            result.Add(new CardItem
                            {
                                Id = AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title = item.GetProperty("name").GetString()!,
                                ImageUrl = image,
                                Subtitle = item.GetProperty("description").GetString()
                            });
                            break;
                        case "album":
                            result.Add(new CardItem
                            {
                                Id = AudioId.FromUri(item.GetProperty("uri").GetString()),
                                Title = item.GetProperty("name").GetString()!,
                                ImageUrl = image,
                                Subtitle = $"{item.GetProperty("total_tracks").GetInt32()} tracks"
                            });
                            break;
                        case "artist":
                            var uri = AudioId.FromUri(item.GetProperty("uri").GetString());
                            result.Add(new CardItem
                            {
                                Id = uri,
                                Title = item.GetProperty("name").GetString()!,
                                ImageUrl = image,
                                Subtitle = item.GetProperty("followers").GetProperty("total").GetInt32().ToString()
                            });
                            break;
                        default:
                            break;
                    }
                }

                var title = group.GetProperty("name").GetString();
                var tagline = group.TryGetProperty("tag_line", out var t) ? t.GetString() : null;
                var groupResult = new HomeGroup
                {
                    Items = result,
                    Title = title,
                    Subtitle = tagline,
                    Id = null
                };
                groupResults.Add(groupResult);
            }
        }

        home.Dispose();
        return groupResults;
    }
}