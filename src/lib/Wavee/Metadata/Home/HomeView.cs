using System.Diagnostics;
using LanguageExt;
using System.Text.Json;
using Serilog;
using Wavee.Id;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;

namespace Wavee.Metadata.Home;

public sealed class HomeView
{
    public required string Greeting { get; init; }
    public required IEnumerable<HomeGroupSection> Sections { get; init; }

    public static HomeView ParseFrom(ReadOnlyMemory<byte> data)
    {
        using var jsondocument = JsonDocument.Parse(data);
        var root = jsondocument.RootElement.GetProperty("data").GetProperty("home");

        var greeting = root.GetProperty("greeting").GetProperty("text").GetString()!;
        var sections = root.GetProperty("sectionContainer").GetProperty("sections").GetProperty("items");
        var output = new HomeGroupSection[sections.GetArrayLength()];
        int i = -1;
        using var arr = sections.EnumerateArray();
        while (arr.MoveNext())
        {
            i++;
            var section = arr.Current;
            var sectionId = SpotifyId.FromUri(section.GetProperty("uri").GetString()!.AsSpan());
            var sectionItems = section.GetProperty("sectionItems");
            var totalCount = sectionItems.GetProperty("totalCount").GetUInt32();
            var items = sectionItems.GetProperty("items");
            var outputItems = new IHomeItem[items.GetArrayLength()];
            int j = -1;

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

            using var itemsArr = items.EnumerateArray();
            while (itemsArr.MoveNext())
            {
                j++;
                var rootItem = itemsArr.Current;
                var uri = rootItem.GetProperty("uri").GetString()!;
                if (uri is "spotify:user:anonymized:collection" or "spotify:collection:tracks")
                {
                    outputItems[j] = new SpotifyCollectionItem();
                    continue;
                }

                var id = SpotifyId.FromUri(uri.AsSpan());
                var type = id.Type;

                try
                {
                    var item = rootItem.GetProperty("content").GetProperty("data");
                    if (item.GetProperty("__typename").GetString() is "NotFound")
                    {
                        Log.Warning("Item {0} not found", id);
                        continue;
                    }

                    switch (type)
                    {
                        case AudioItemType.Playlist:
                            {
                                var name = item.GetProperty("name").GetString()!;
                                var images = ParseImages(item.GetProperty("images").GetProperty("items").EnumerateArray()
                                    .First().GetProperty("sources"));
                                var description = item.TryGetProperty("description", out var desc) &&
                                                  desc.ValueKind is JsonValueKind.String
                                    ? desc.GetString()
                                    : Option<string>.None;
                                var ownerName = item.GetProperty("ownerV2").GetProperty("data").GetProperty("name")
                                    .GetString()!;

                                outputItems[j] = new PlaylistHomeItem
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
                                var artists = item.GetProperty("artists").GetProperty("items").EnumerateArray().Select(x =>
                                    new TrackArtist
                                    {
                                        Id = SpotifyId.FromUri(x.GetProperty("uri").GetString()!.AsSpan()),
                                        Name = x.GetProperty("profile").GetProperty("name").GetString()!
                                    }).ToArray();

                                outputItems[j] = new AlbumHomeItem
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

                                outputItems[j] = new ArtistHomeItem
                                {
                                    Id = id,
                                    Name = name,
                                    Images = images
                                };
                                break;
                            }
                    }
                }
                catch (KeyNotFoundException)
                {
                    Log.Warning("Item {0} not found", id);
                }
            }

            output[i] = new HomeGroupSection
            {
                SectionId = sectionId,
                TotalCount = totalCount,
                Items = outputItems
            };
        }

        return new HomeView
        {
            Greeting = greeting,
            Sections = output
        };
    }
}

public sealed class HomeGroupSection
{
    public required SpotifyId SectionId { get; init; }
    public required uint TotalCount { get; init; }
    public required IEnumerable<IHomeItem> Items { get; init; }
}

public interface IHomeItem
{
    SpotifyId Id { get; }
}

public sealed class SpotifyCollectionItem : IHomeItem
{
    public SpotifyId Id => new SpotifyId(0, AudioItemType.UserCollection, ServiceType.Spotify);
}
public sealed class PlaylistHomeItem : IHomeItem
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required Option<string> Description { get; init; }
    public required CoverImage[] Images { get; init; }
    public required string OwnerName { get; init; }
}

public sealed class AlbumHomeItem : IHomeItem
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required TrackArtist[] Artists { get; init; }
    public required CoverImage[] Images { get; init; }
}

public sealed class ArtistHomeItem : IHomeItem
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required CoverImage[] Images { get; init; }
}