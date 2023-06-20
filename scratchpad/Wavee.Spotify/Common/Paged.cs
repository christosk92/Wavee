using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Spotify.Metadata;
using Wavee.Spotify.Artist;

namespace Wavee.Spotify.Common;

/// <summary>
/// Represents a paged collection of items.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class Paged<T>
{
    /// <summary>
    /// All items in the collection in the current page.
    /// </summary>
    public required IReadOnlyCollection<T> Items { get; init; }

    /// <summary>
    /// A boolean indicating whether there are more items in the collection.
    /// </summary>
    public required bool HasNext { get; init; }


    /// <summary>
    /// The total number of items in the collection.
    /// </summary>
    public required long Total { get; init; }

    internal static Paged<SpotifyArtistDiscographyGroup> ParsePagedDiscographyFrom(Stream stream,
        DiscographyType discographyType, int currentOffset,
        int currentLimit)
    {
        using var json = JsonDocument.Parse(stream);
        try
        {
            var discography = json.RootElement
                .GetProperty("data")
                .GetProperty("artistUnion")
                .GetProperty("discography");

            var artist = discographyType switch
            {
                DiscographyType.Albums => discography.GetProperty("albums"),
                DiscographyType.Singles => discography.GetProperty("singles"),
                DiscographyType.Compilations => discography.GetProperty("compilations"),
            };

            var totalCount = artist.GetProperty("totalCount").GetInt64();
            var hasNext = currentOffset + currentLimit < totalCount;
            using var items = artist.GetProperty("items").EnumerateArray();

            static void Parse(JsonElement item,
                Dictionary<DiscographyType, List<SpotifyArtistDiscographyReleaseWrapper>> output)
            {
                using var releases = item.GetProperty("releases").GetProperty("items").EnumerateArray();
                var release = releases.First();

                var uri = release.GetProperty("uri").GetString();
                var name = release.GetProperty("name").GetString();
                var type = release.GetProperty("type").GetString();
                var year = release.GetProperty("date").GetProperty("year").GetUInt16();

                var coverArt = release.GetProperty("coverArt").GetProperty("sources").EnumerateArray().First();
                var coverArtUri = coverArt.GetProperty("url").GetString();
                var tracksCount = release.GetProperty("tracks").GetProperty("totalCount").GetUInt32();

                var discographyType = type switch
                {
                    "ALBUM" => DiscographyType.Albums,
                    "SINGLE" => DiscographyType.Singles,
                    "COMPILATION" => DiscographyType.Compilations,
                };

                var discographyGroup = output[discographyType];
                var discographyRelease = new SpotifyArtistDiscographyRelease(
                    Id: SpotifyId.FromUri(uri),
                    Name: name,
                    ImageUrl: coverArtUri,
                    TotalTracks: tracksCount,
                    Year: year,
                    Month: null,
                    Day: null
                );
                discographyGroup.Add(new SpotifyArtistDiscographyReleaseWrapper(
                    Initialized: true,
                    Value: discographyRelease
                ));
            }

            var result = new Dictionary<DiscographyType, List<SpotifyArtistDiscographyReleaseWrapper>>();
            result[DiscographyType.Albums] = new();
            result[DiscographyType.Singles] = new();
            result[DiscographyType.Compilations] = new();

            foreach (var item in items)
            {
                Parse(item, result);
            }

            return new Paged<SpotifyArtistDiscographyGroup>
            {
                Items = result.Select(c => new SpotifyArtistDiscographyGroup(
                    Type: c.Key,
                    Items: c.Value.ToArray()
                )).ToImmutableList(),
                HasNext = hasNext,
                Total = totalCount
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Debugger.Break();
            Debug.WriteLine(e);
            throw;
        }
    }
}