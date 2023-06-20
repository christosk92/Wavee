using System.Diagnostics;
using System.Text.Json;
using LanguageExt;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;

namespace Wavee.UI.Core.Contracts.Artist;

public interface IArtistView
{
    Aff<SpotifyArtistViewV2> GetArtistViewAsync(AudioId id, CancellationToken ct = default);
}

public sealed class SpotifyArtistViewV2
{
    public bool Verified { get; set; }
    public AudioId Id { get; set; }
    public string Name { get; set; }
    public string ProfileImageUrl { get; set; }
    public string? HeaderImageUrl { get; set; }

    public List<SpotifyArtistDiscographyGroupV2> Discography { get; set; }
    public List<SpotifyArtistTopTrackV2> TopTracks { get; set; }
    public ulong MonthlyListeners { get; set; }

    public static SpotifyArtistViewV2 ParseFrom(Stream response)
    {
        using var json = JsonDocument.Parse(response);
        try
        {

            var artist = json.RootElement.GetProperty("data").GetProperty("artistUnion");

            var uri = artist.GetProperty("uri").GetString();

            var profile = artist.GetProperty("profile");


            var name = profile.GetProperty("name").GetString();
            var verified = profile.GetProperty("verified").GetBoolean();

            //TODO: Playlists (playlistsV2)

            var visuals = artist.GetProperty("visuals");


            static string? ParseImage(JsonElement visuals)
            {
                if (visuals.ValueKind is JsonValueKind.Null) return null;
                if (visuals.TryGetProperty("sources", out var images))
                {
                    using var imagesArr = images.EnumerateArray();
                    if (imagesArr.Any())
                    {
                        return imagesArr.First().GetProperty("url").GetString();
                    }

                    return null;
                }

                return null;
            }

            var avatarImage = ParseImage(visuals.GetProperty("avatarImage"));
            var headerImage = ParseImage(visuals.GetProperty("headerImage"));

            var discography = artist.GetProperty("discography");


            static void GetView(JsonElement releases,
                DiscographyType type,
                bool canSwitchViews,
                bool alwaysHorizontal,
                List<SpotifyArtistDiscographyGroupV2> output)
            {
                var key = type switch
                {
                    DiscographyType.Albums => "albums",
                    DiscographyType.Singles => "singles",
                    DiscographyType.Compilations => "compilations",
                };
                var release = releases.GetProperty(key);
                var totalReleases = release.GetProperty("totalCount").GetInt32();
                if (totalReleases == 0)
                {
                    return;
                }

                using var items = release.GetProperty("items").EnumerateArray();
                var results = new SpotifyArtistDiscographyV2[totalReleases];
                for (int i = 0; i < totalReleases; i++)
                {
                    if (!items.MoveNext())
                    {
                        //we are done fill the rest with empty
                        results[i] = new SpotifyArtistDiscographyV2
                        {
                            Initialized = false,
                            Value = null
                        };
                        continue;
                    }

                    var item = items.Current;
                    using var subReleases = item.GetProperty("releases").GetProperty("items").EnumerateArray();
                    var actualRelease = subReleases.First();

                    var uri = actualRelease.GetProperty("uri").GetString();
                    var name = actualRelease.GetProperty("name").GetString();
                    var date = actualRelease.GetProperty("date");
                    using var cover = actualRelease.GetProperty("coverArt").GetProperty("sources").EnumerateArray();
                    var biggestImage = cover.First().GetProperty("url").GetString();
                    var totalTracks = actualRelease.GetProperty("tracks").GetProperty("totalCount").GetUInt32();

                    var releaseDatePrecisionStr = date.GetProperty("precision").GetString();
                    var releaseDatePrecisionType = releaseDatePrecisionStr switch
                    {
                        "YEAR" => ReleaseDatePrecisionType.Year,
                        "MONTH" => ReleaseDatePrecisionType.Month,
                        "DAY" => ReleaseDatePrecisionType.Day,
                        _ => throw new ArgumentOutOfRangeException(nameof(releaseDatePrecisionStr))
                    };
                    results[i] = new SpotifyArtistDiscographyV2
                    {
                        Initialized = true,
                        Value = new SpotifyArtistDiscographyItemV2
                        {
                            Id = AudioId.FromUri(uri),
                            Name = name,
                            ImageUrl = biggestImage,
                            TotalTracks = totalTracks,
                            Year = date.GetProperty("year").GetUInt16(),
                            Month = releaseDatePrecisionType >= ReleaseDatePrecisionType.Month
                                ? date.GetProperty("month").GetUInt16()
                                : null,
                            Day = releaseDatePrecisionType >= ReleaseDatePrecisionType.Day
                                ? date.GetProperty("day").GetUInt16()
                                : null
                        }
                    };
                }

                output.Add(new SpotifyArtistDiscographyGroupV2
                {
                    Type = type,
                    Items = results,
                    AlwaysHorizontal = alwaysHorizontal,
                    CanSwitchTemplates = canSwitchViews
                });
            }

            var res = new List<SpotifyArtistDiscographyGroupV2>(3);
            GetView(discography, DiscographyType.Albums, true, false, res);
            GetView(discography, DiscographyType.Singles, true, false, res);
            GetView(discography, DiscographyType.Compilations, false, false, res);


            var topTracks = discography.GetProperty("topTracks");
            using var topTracksEnum = topTracks.GetProperty("items").EnumerateArray();
            var results = new List<SpotifyArtistTopTrackV2>();

            ushort index = 0;
            foreach (var topTrack in topTracksEnum)
            {
                var uid = topTrack.GetProperty("uid").GetString();
                var track = topTrack.GetProperty("track");

                var topTrackName = track.GetProperty("name").GetString();
                var id = AudioId.FromUri(track.GetProperty("uri").GetString());
                var playcountStr = track.TryGetProperty("playcount", out var plc)
                    ? (plc.ValueKind == JsonValueKind.Null ? (string?)null : plc.GetString())
                    : null;
                var playcount = playcountStr != null ? long.Parse(playcountStr) : (long?)null;
                var duration =
                    TimeSpan.FromMilliseconds(
                        track.GetProperty("duration").GetProperty("totalMilliseconds").GetDouble());
                var artists = track.GetProperty("artists").GetProperty("items").EnumerateArray().Select(x =>
                    new SpotifyAlbumArtistView
                    {
                        Id = AudioId.FromUri(x.GetProperty("uri").GetString()),
                        Name = x.GetProperty("profile").GetProperty("name").GetString()
                    }).ToArray();

                var album = track.GetProperty("albumOfTrack");
                var albumId = AudioId.FromUri(album.GetProperty("uri").GetString());
                using var coverArt = album.GetProperty("coverArt").GetProperty("sources").EnumerateArray();
                var coverArtUrl = coverArt.First().GetProperty("url").GetString();

                results.Add(new SpotifyArtistTopTrackV2
                {
                    Uid = uid,
                    Name = topTrackName,
                    Id = id,
                    Playcount = playcount,
                    Duration = duration,
                    Artists = artists,
                    Album = new SpotifyTrackAlbumShort
                    {
                        Id = albumId,
                        ImageUrl = coverArtUrl
                    },
                    Index = index++
                });
            }


            var stats = artist.GetProperty("stats");
            var monthlyListeners = stats.GetProperty("monthlyListeners").GetUInt64();
            return new SpotifyArtistViewV2
            {
                Name = name,
                ProfileImageUrl = avatarImage,
                Discography = res,
                TopTracks = results,
                HeaderImageUrl = headerImage,
                Verified = verified,
                Id = AudioId.FromUri(uri),
                MonthlyListeners = monthlyListeners
            };
        }
        catch (Exception e)
        {
            //"   at System.Text.Json.ThrowHelper.ThrowJsonElementWrongTypeException(JsonTokenType expectedType, JsonTokenType actualType)\r\n   at System.Text.Json.JsonDocument.TryGetNamedPropertyValue(Int32 index, ReadOnlySpan`1 propertyName, JsonElement& value)\r\n   at System.Text.Json.JsonElement.TryGetProperty(String propertyName, JsonElement& value)\r\n   at Wavee.UI.Core.Contracts.Artist.SpotifyArtistViewV2.<ParseFrom>g__ParseImage|32_0(JsonElement visuals) in C:\\Users\\chris-pc\\dev\\personal\\Wavee\\src\\Wavee.UI\\Core\\Contracts\\Artist\\IArtistView.cs:line 50\r\n   at Wavee.UI.Core.Contracts.Artist.SpotifyArtistViewV2.ParseFrom(Stream response) in C:\\Users\\chris-pc\\dev\\personal\\Wavee\\src\\Wavee.UI\\Core\\Contracts\\Artist\\IArtistView.cs:line 65"
            Console.WriteLine(e);
            Debug.WriteLine(e);
            throw;
        }
        finally
        {
            response.Dispose();
        }
    }

    public static SpotifyArtistViewV2 FromCache(ReadOnlyMemory<byte> readOnlyMemory)
    {
        return JsonSerializer.Deserialize<SpotifyArtistViewV2>(readOnlyMemory.Span)!;
    }
}

public sealed class SpotifyArtistTopTrackV2
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public AudioId Id { get; set; }
    public long? Playcount { get; set; }
    public TimeSpan Duration { get; set; }
    public SpotifyAlbumArtistView[] Artists { get; set; }
    public SpotifyTrackAlbumShort Album { get; set; }
    public ushort Index { get; set; }

    public ushort MinusOne(ushort v)
    {
        return (ushort)(v - 1);
    }

    public bool Negate(bool b)
    {
        return !b;
    }

    public string FormatPlaycount(long? playcount)
    {
        return playcount.HasValue
            ? playcount.Value.ToString("N0")
            : "< 1,000";
    }

    public string FormatTimestamp(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"mm\:ss");
    }
}

public class SpotifyTrackAlbumShort
{
    public AudioId Id { get; set; }
    public string? ImageUrl { get; set; }
}

public class SpotifyAlbumArtistView
{
    public AudioId Id { get; set; }
    public string Name { get; set; }
    public string? Image { get; set; }
}

public sealed class SpotifyArtistDiscographyGroupV2
{
    public DiscographyType Type { get; set; }
    public SpotifyArtistDiscographyV2[] Items { get; set; }
    public bool AlwaysHorizontal { get; set; }
    public string GroupName => Type switch
    {
        DiscographyType.Albums => "Albums",
        DiscographyType.Singles => "Singles and EPs",
        DiscographyType.Compilations => "Compilations",
        _ => throw new ArgumentOutOfRangeException()
    };

    public bool CanSwitchTemplates { get; set; }
}
public sealed class SpotifyArtistDiscographyV2
{
    public bool Initialized { get; set; }
    public SpotifyArtistDiscographyItemV2? Value { get; set; }

    public bool Negate(bool b)
    {
        return !b;
    }
}

public enum DiscographyType
{
    Albums,
    Singles,
    Compilations
}

public sealed class SpotifyArtistDiscographyItemV2
{
    public AudioId Id { get; set; }
    public string Name { get; set; }
    public string? ImageUrl { get; set; }
    public uint TotalTracks { get; set; }
    public ushort Year { get; set; }
    public ushort? Month { get; set; }
    public ushort? Day { get; set; }
    public string ReleaseDateAsStr => Month.HasValue && Day.HasValue
        ? $"{Year}-{Month.Value:D2}-{Day.Value:D2}"
        : $"{Year}";
}