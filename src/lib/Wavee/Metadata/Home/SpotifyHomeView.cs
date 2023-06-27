using System.Text.Json;
using System.Xml.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Serilog;
using Wavee.Id;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;

namespace Wavee.Metadata.Home;

public sealed class SpotifyHomeView
{
    public required string Greeting { get; init; }
    public required IEnumerable<SpotifyHomeGroupSection> Sections { get; init; }

    public static SpotifyHomeView ParseFrom(ReadOnlyMemory<byte> data, SpotifyHomeGroupSection recentlyPlayed, Option<AudioItemType> typeFilterType)
    {
        try
        {
            using var jsondocument = JsonDocument.Parse(data);
            var root = jsondocument.RootElement.GetProperty("data").GetProperty("home");

            var greeting = root.GetProperty("greeting").GetProperty("text").GetString()!;
            var sections = root.GetProperty("sectionContainer").GetProperty("sections").GetProperty("items");
            var output = new SpotifyHomeGroupSection[sections.GetArrayLength()];
            int i = -1;
            using var arr = sections.EnumerateArray();
            while (arr.MoveNext())
            {
                i++;
                var section = arr.Current;
                var sectionId = SpotifyId.FromUri(section.GetProperty("uri").GetString()!.AsSpan());
                var title = string.Empty;
                if (section.TryGetProperty("data", out var dt)
                    && dt.TryGetProperty("title", out var titleProp)
                    && titleProp.TryGetProperty("text", out var txt))
                {

                    title = txt.GetString()!;
                }

                var sectionType = dt.GetProperty("__typename").GetString()!;
                if (sectionType is "HomeRecentlyPlayedSectionData")
                {
                    recentlyPlayed.Title = title;
                    output[i] = recentlyPlayed;
                    continue;
                }

                var sectionItems = section.GetProperty("sectionItems");
                var totalCount = sectionItems.GetProperty("totalCount").GetUInt32();
                var items = sectionItems.GetProperty("items");
                var outputItems = new ISpotifyHomeItem[items.GetArrayLength()];
                int j = -1;

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

                    var item = rootItem.GetProperty("content").GetProperty("data");

                    var homeitem = SpotifyItemParser.ParseFrom(item);
                    if (homeitem.IsSome)
                    {
                        outputItems[j] = homeitem.ValueUnsafe();
                    }
                }

                output[i] = new SpotifyHomeGroupSection
                {
                    SectionId = sectionId,
                    TotalCount = totalCount,
                    Items = outputItems.Where(x => x is not null && (typeFilterType.IsNone || typeFilterType.ValueUnsafe().HasFlag(x.Id.Type))),
                    Title = title
                };
            }

            return new SpotifyHomeView
            {
                Greeting = greeting,
                Sections = output
            };
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to parse home view");
            throw;
        }
    }
}