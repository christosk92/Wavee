using System.Diagnostics;
using System.Text.Json;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Artist;

/// <summary>
/// Represents a Spotify artist.
/// </summary>
/// <param name="Id">
/// The <see cref="SpotifyId"/> of the artist.
/// </param>
/// <param name="Name">
/// The name of the artist.
/// </param>
/// <param name="IsVerified">
/// A value indicating whether the artist is verified.
/// </param>
/// <param name="ProfilePicture">
/// The URL to the profile picture of the artist.
/// </param>
/// <param name="HeaderPicture">
/// The URL to the header picture of the artist.
/// </param>
/// <param name="Discography">
/// A collection of <see cref="SpotifyArtistDiscographyGroup"/> representing the discography of the artist.
/// </param>
/// <param name="TopTracks">
/// A collection of <see cref="SpotifyArtistTopTrack"/> representing the top tracks of the artist.
/// </param>
/// <param name="MonthlyListeners">
/// The monthly listeners of the artist.
/// </param>
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
    internal static SpotifyArtist ParseFrom(Stream stream)
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

/// <summary>
/// A track from the top tracks of an artist
/// </summary>
/// <param name="Uid">
/// The unique id of the track. Note that this is not the same as the <see cref="SpotifyId"/> and is only used for uniquely identifying the track in the top tracks list.
/// </param>
/// <param name="Name">
/// The name of the track
/// </param>
/// <param name="Id">
/// The <see cref="SpotifyId"/> of the track
/// </param>
/// <param name="Playcount">
///  The playcount of the track. If this is null, the track has been played less than 1000 times.
/// </param>
/// <param name="Duration">
/// The duration of the track
/// </param>
/// <param name="Artists">
///  All artists that contributed to the track
/// </param>
/// <param name="Album">
/// The album the track is from
/// </param>
/// <param name="Index">
///  The index of the track in the top tracks list
/// </param>
public sealed record SpotifyArtistTopTrack(string Uid,
    string Name, SpotifyId Id, long? Playcount,
    TimeSpan Duration,
    SpotifyAlbumArtist[] Artists,
    SpotifyTrackAlbumShort Album, ushort Index);

/// <summary>
/// Describes a group of releases in an artist's discography.
/// Note that the <see cref="Items"/> array may contain null values.
/// </summary>
/// <param name="Type">
/// The type of the group (e.g. albums, singles, compilations)
/// </param>
/// <param name="Items">
///  An array of releases. Note that this array may contain null values. Check the <see cref="SpotifyArtistDiscographyReleaseWrapper.Initialized"/> property to see if the value is null.
/// </param>
public sealed record SpotifyArtistDiscographyGroup
    (DiscographyType Type, SpotifyArtistDiscographyReleaseWrapper[] Items);

/// <summary>
/// A particular release in an artist's discography.
/// </summary>
/// <param name="Initialized">
/// If this is false, the <see cref="Value"/> property is null.
/// The reason for this is that the Spotify API does not return all releases in an artist's discography.
/// Which means some pagination is required to get all releases.
/// </param>
/// <param name="Value">
/// The release. This is null if <see cref="Initialized"/> is false.
/// </param>
public sealed record SpotifyArtistDiscographyReleaseWrapper(bool Initialized, SpotifyArtistDiscographyRelease? Value);

/// <summary>
/// The release of a particular album/single/compilation in an artist's discography.
/// </summary>
/// <param name="Id">
/// The <see cref="SpotifyId"/> of the release
/// </param>
/// <param name="Name">
/// The name of the release
///  </param>
/// <param name="ImageUrl">
/// The url of the image of the release
/// </param>
/// <param name="TotalTracks">
///  The total number of tracks in the release
/// </param>
/// <param name="Year">
/// The year the release was released
/// </param>
/// <param name="Month">
/// The month the release was released. This is null if the month is unknown.
/// </param>
/// <param name="Day">
/// The day the release was released. This is null if the day is unknown.
/// </param>
public sealed record SpotifyArtistDiscographyRelease(
    SpotifyId Id,
    string Name,
    string ImageUrl,
    uint TotalTracks,
    ushort Year,
    ushort? Month,
    ushort? Day);

/// <summary>
/// The type of a discography group
/// </summary>
public enum DiscographyType
{
    /// <summary>
    /// Albums
    /// </summary>
    Albums,

    /// <summary>
    /// Singles and EPs
    /// </summary>
    Singles,

    /// <summary>
    /// Compilations
    /// </summary>
    Compilations
}

/// <summary>
/// The type of a release date
/// </summary>
public enum ReleaseDatePrecisionType
{
    /// <summary>
    /// No release date
    /// </summary>
    Unknown,

    /// <summary>
    /// The release date is only known to the year
    /// </summary>
    Year,

    /// <summary>
    /// The release date is only known to the month an
    /// </summary>
    Month,

    /// <summary>
    /// The full release date is known
    /// </summary>
    Day
}