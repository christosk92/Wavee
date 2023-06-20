using System.Diagnostics;
using System.Text.Json;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Artist;

public sealed record SpotifyArtist(
    SpotifyId Id,
    string Name,
    bool IsVerified,
    string ProfilePicture,
    string? HeaderPicture,
    IReadOnlyCollection<SpotifyArtistDiscographyGroup> Discography,
    IReadOnlyCollection<SpotifyArtistTopTrack> TopTracks,
    ulong MonthlyListeners)
{
    public static SpotifyArtist ParseFrom(Stream stream)
    {
        using var json = JsonDocument.Parse(stream);

        try
        {
            var artist = json.RootElement.GetProperty("data").GetProperty("artistUnion");
            var uri = artist.GetProperty("uri").GetString()!;
            var profile = artist.GetProperty("profile");

            var name = profile.GetProperty("name").GetString()!;
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
                List<SpotifyArtistDiscographyGroup> output)
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
                var results = new SpotifyArtistDiscographyReleaseWrapper[totalReleases];
                for (int i = 0; i < totalReleases; i++)
                {
                    if (!items.MoveNext())
                    {
                        //we are done fill the rest with empty
                        results[i] = new SpotifyArtistDiscographyReleaseWrapper
                        (
                            Initialized: false,
                            Value: null
                        );
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
                    results[i] = new SpotifyArtistDiscographyReleaseWrapper
                    (
                        Initialized: true,
                        Value: new SpotifyArtistDiscographyRelease(
                            Id: SpotifyId.FromUri(uri),
                            Name: name,
                            ImageUrl: biggestImage,
                            TotalTracks: totalTracks,
                            Year: date.GetProperty("year").GetUInt16(),
                            Month: releaseDatePrecisionType >= ReleaseDatePrecisionType.Month
                                ? date.GetProperty("month").GetUInt16()
                                : null,
                            Day: releaseDatePrecisionType >= ReleaseDatePrecisionType.Day
                                ? date.GetProperty("day").GetUInt16()
                                : null
                        )
                    );
                }

                output.Add(new SpotifyArtistDiscographyGroup
                (
                    Type: type,
                    Items: results
                ));
            }

            var res = new List<SpotifyArtistDiscographyGroup>(3);
            GetView(discography, DiscographyType.Albums, true, false, res);
            GetView(discography, DiscographyType.Singles, true, false, res);
            GetView(discography, DiscographyType.Compilations, false, false, res);


            var topTracks = discography.GetProperty("topTracks");
            using var topTracksEnum = topTracks.GetProperty("items").EnumerateArray();
            var results = new List<SpotifyArtistTopTrack>();

            ushort index = 0;
            foreach (var topTrack in topTracksEnum)
            {
                var uid = topTrack.GetProperty("uid").GetString();
                var track = topTrack.GetProperty("track");

                var topTrackName = track.GetProperty("name").GetString();
                var id = SpotifyId.FromUri(track.GetProperty("uri").GetString());
                var playcountStr = track.TryGetProperty("playcount", out var plc)
                    ? (plc.ValueKind == JsonValueKind.Null ? (string?)null : plc.GetString())
                    : null;
                var playcount = playcountStr != null ? long.Parse(playcountStr) : (long?)null;
                var duration =
                    TimeSpan.FromMilliseconds(
                        track.GetProperty("duration").GetProperty("totalMilliseconds").GetDouble());

                var artists = track.GetProperty("artists").GetProperty("items").EnumerateArray().Select(x =>
                    new SpotifyAlbumArtist
                    (
                        Id: SpotifyId.FromUri(x.GetProperty("uri").GetString()),
                        Name: x.GetProperty("profile").GetProperty("name").GetString()!,
                        Image: null
                    )).ToArray();

                var album = track.GetProperty("albumOfTrack");
                var albumId = SpotifyId.FromUri(album.GetProperty("uri").GetString());
                using var coverArt = album.GetProperty("coverArt").GetProperty("sources").EnumerateArray();
                var coverArtUrl = coverArt.First().GetProperty("url").GetString();

                results.Add(new SpotifyArtistTopTrack
                (
                    Uid: uid,
                    Name: topTrackName,
                    Id: id,
                    Playcount: playcount,
                    Duration: duration,
                    Artists: artists,
                    Album: new SpotifyTrackAlbumShort
                    (
                        Id: albumId,
                        Name: "",
                        Image: coverArtUrl
                    ),
                    Index: index++
                ));
            }


            var stats = artist.GetProperty("stats");
            var monthlyListeners = stats.GetProperty("monthlyListeners").GetUInt64();

            return new SpotifyArtist
            (
                Id: SpotifyId.FromUri(artist.GetProperty("uri").GetString()),
                Name: artist.GetProperty("profile").GetProperty("name").GetString()!,
                IsVerified: verified,
                ProfilePicture: avatarImage,
                HeaderPicture: headerImage,
                TopTracks: results,
                Discography: res,
                MonthlyListeners: monthlyListeners
            );
        }
        catch
            (Exception e)
        {
            Console.WriteLine(e);
            Debug.WriteLine(e);
            Debugger.Break();
            throw;
        }
    }
}

public sealed record SpotifyArtistTopTrack(string Uid,
    string Name, SpotifyId Id, long? Playcount,
    TimeSpan Duration,
    SpotifyAlbumArtist[] Artists,
    SpotifyTrackAlbumShort Album, ushort Index);

public sealed record SpotifyArtistDiscographyGroup
    (DiscographyType Type, SpotifyArtistDiscographyReleaseWrapper[] Items);

public sealed record SpotifyArtistDiscographyReleaseWrapper(bool Initialized,
    SpotifyArtistDiscographyRelease? Value);

public sealed record SpotifyArtistDiscographyRelease(
    SpotifyId Id,
    string Name,
    string ImageUrl,
    uint TotalTracks,
    ushort Year,
    ushort? Month,
    ushort? Day);

public enum DiscographyType
{
    Albums,
    Singles,
    Compilations
}

public enum ReleaseDatePrecisionType
{
    Unknown,
    Year,
    Month,
    Day
}