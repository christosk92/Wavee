using System.Text.Json;
using Eum.Spotify.playlist4;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Metadata.Common;

namespace Wavee.Metadata.Artist;

public readonly record struct ArtistOverview(SpotifyId Id, bool IsSaved, Uri SharingUrl, ArtistOverviewProfile Profile,
    ArtistVisuals Visuals, ArtistDiscography Discography, ArtistStats Statistics, ArtistRelatedContent RelatedContent,
    ArtistGoods Goods)
{
    internal static ArtistOverview ParseFrom(ReadOnlyMemory<byte> data)
    {
        using var jsonDocument = JsonDocument.Parse(data);
        var artist = jsonDocument.RootElement.GetProperty("data").GetProperty("artistUnion");
        var uri = SpotifyId.FromUri(artist.GetProperty("uri").GetString().AsSpan());
        var saved = artist.GetProperty("saved").GetBoolean();
        var sharingUrl = artist.GetProperty("sharingInfo").GetProperty("shareUrl").GetString()!;

        var profile = ParseArtistProfile(artist.GetProperty("profile"));
        var visuals = ParseArtistVisuals(artist.GetProperty("visuals"));
        var discography = ParseArtistDiscography(artist.GetProperty("discography"));
        var stats = ParseArtistStats(artist.GetProperty("stats"));
        var relatedContent = ParseArtistRelatedContent(artist.GetProperty("relatedContent"));
        var goods = ParseArtistGoods(artist.GetProperty("goods"));

        return new ArtistOverview(
            Id: uri,
            IsSaved: saved,
            SharingUrl: new Uri(sharingUrl),
            Profile: profile,
            Visuals: visuals,
            Discography: discography,
            Statistics: stats,
            RelatedContent: relatedContent,
            Goods: goods
        );
    }

    private static ArtistRelatedContent ParseArtistRelatedContent(JsonElement getProperty)
    {
        return new ArtistRelatedContent();
    }

    private static ArtistGoods ParseArtistGoods(JsonElement getProperty)
    {
        return new ArtistGoods();
    }

    private static ArtistStats ParseArtistStats(JsonElement stats)
    {
        var followers = stats.GetProperty("followers").GetUInt64();
        var monthlyListeners = stats.GetProperty("monthlyListeners").GetUInt64();
        var worldRank = stats.GetProperty("worldRank").GetUInt16();
        return new ArtistStats(
            Followers: followers,
            MonthlyListeners: monthlyListeners,
            WorldRank: worldRank is 0 ? Option<ushort>.None : worldRank
        );
    }

    private static ArtistDiscography ParseArtistDiscography(JsonElement discography)
    {
        Option<ArtistDiscographyRelease> latestRelease = Option<ArtistDiscographyRelease>.None;
        if (discography.TryGetProperty("latest", out var potentialLatest) &&
            potentialLatest.ValueKind is not JsonValueKind.Null)
        {
            latestRelease = ParseRelease(potentialLatest);
        }

        var albums = ParseDiscographyGroup(discography.GetProperty("albums"));
        var singles = ParseDiscographyGroup(discography.GetProperty("singles"));
        var compilations = ParseDiscographyGroup(discography.GetProperty("compilations"));
        var topTracks = ParseTopTracks(discography.GetProperty("topTracks").GetProperty("items"));
        return new ArtistDiscography(
            LatestRelease: latestRelease,
            Albums: albums,
            Singles: singles,
            Compilations: compilations,
            TopTracks: topTracks
        );
    }

    private static Option<ArtistDiscographyRelease>[] ParseDiscographyGroup(JsonElement group)
    {
        var totalCount = group.GetProperty("totalCount").GetUInt16();
        var output = new Option<ArtistDiscographyRelease>[totalCount];
        for (int k = 0; k < totalCount; k++)
        {
            output[k] = Option<ArtistDiscographyRelease>.None;
        }

        using var items = group.GetProperty("items").EnumerateArray();
        int i = 0;
        while (items.MoveNext())
        {
            var current = items.Current;

            //items are wrapped inside releases array
            using var releasesOfRelease = current.GetProperty("releases").GetProperty("items").EnumerateArray();
            if (!releasesOfRelease.MoveNext())
            {
                i++;
                continue;
            }

            var releaseOfRelease = releasesOfRelease.Current;

            var parsedRelease = ParseRelease(releaseOfRelease);
            output[i] = Option<ArtistDiscographyRelease>.Some(parsedRelease);

            i++;
        }

        return output;
    }

    private static ArtistTopTrack[] ParseTopTracks(JsonElement toptracks)
    {
        using var items = toptracks.EnumerateArray();
        var output = new ArtistTopTrack[toptracks.GetArrayLength()];
        int i = 0;
        while (items.MoveNext())
        {
            var current = items.Current;
            var uid = current.GetProperty("uid").GetString()!;
            var track = current.GetProperty("track");

            var name = track.GetProperty("name").GetString()!;
            //var playcount = track.GetProperty("playcount").tryget(out var plc) ? plc : Option<ulong>.None;
            Option<ulong> playcount = Option<ulong>.None;
            if (track.TryGetProperty("playcount", out var potplc) && potplc.ValueKind is JsonValueKind.String)
            {
                var val = potplc.GetString();
                if (ulong.TryParse(val, out var playcoun))
                {
                    playcount = playcoun;
                }
            }

            var discNumber = track.GetProperty("discNumber").GetUInt16();
            var duration =
                TimeSpan.FromMilliseconds(track.GetProperty("duration").GetProperty("totalMilliseconds").GetDouble());

            var artists = track.GetProperty("artists").GetProperty("items");
            var artistsOutput = new TrackArtist[artists.GetArrayLength()];
            using var arr = artists.EnumerateArray();
            int j = 0;
            while (arr.MoveNext())
            {
                var cr = arr.Current;
                artistsOutput[j] = new TrackArtist(
                    Id: SpotifyId.FromUri(cr.GetProperty("uri").GetString().AsSpan()),
                    Name: cr.GetProperty("profile").GetProperty("name").GetString()!
                );
                j++;
            }

            var album = track.GetProperty("albumOfTrack");
            var albumOutput = new TrackAlbum(
                Id: SpotifyId.FromUri(album.GetProperty("uri").GetString().AsSpan()),
                Images: ParseCoverArt(album.GetProperty("coverArt").GetProperty("sources"))
            );

            var ctr = track.GetProperty("contentRating");
            // var ctrOutput = new ContentRating { };

            output[i] = new ArtistTopTrack(
                Id: SpotifyId.FromUri(track.GetProperty("uri").GetString()!.AsSpan()),
                Uid: uid,
                Name: name,
                Playcount: playcount,
                DiscNumber: discNumber,
                Duration: duration,
                ContentRating: ctr.GetProperty("label")
                        .GetString() switch
                {
                    "NONE" => ContentRatingType.NOTHING,
                    "NINETEEN_PLUS" => ContentRatingType.NINETEEN_PLUS,
                    "EXPLICIT" => ContentRatingType.EXPLICIT,
                    _ => ContentRatingType.UNKNOWN
                },
                Artists: artistsOutput,
                Album: albumOutput
            );
            i++;
        }

        return output;
    }

    internal static ArtistDiscographyRelease ParseRelease(JsonElement release)
    {
        var id = SpotifyId.FromUri(release.GetProperty("uri").GetString().AsSpan());
        var name = release.GetProperty("name").GetString()!;
        var type = release.GetProperty("type").GetString() switch
        {
            "SINGLE" or "EP" => ReleaseType.Single,
            "ALBUM" => ReleaseType.Album,
            "COMPILATION" => ReleaseType.Compilation
        };
        var dt = release.GetProperty("date");
        var precision = dt.TryGetProperty("precision", out var prec) ? prec.GetString() switch
        {
            "DAY" => ReleaseDatePrecisionType.Day,
            "MONTH" => ReleaseDatePrecisionType.Month,
            "YEAR" => ReleaseDatePrecisionType.Year,
            _ => ReleaseDatePrecisionType.Unknown
        } : ReleaseDatePrecisionType.Year;
        var date = new DiscographyReleaseDate(
            new DateTime(
                year: precision >= ReleaseDatePrecisionType.Year ? dt.GetProperty("year").GetInt32() : 0,
                month: precision >= ReleaseDatePrecisionType.Month ? dt.GetProperty("month").GetInt32() : 1,
                day: precision >= ReleaseDatePrecisionType.Day ? dt.GetProperty("day").GetInt32() : 1
            ),
            precision
        );

        var coverArts = ParseCoverArt(release.GetProperty("coverArt").GetProperty("sources"));
        var tracksCount = release.GetProperty("tracks").GetProperty("totalCount").GetUInt16();
        var label = release.TryGetProperty("label", out var lbl) ? lbl.GetString()! : Option<string>.None;

        var copyoutputs = Array.Empty<ReleaseCopyright>();

        if (release.TryGetProperty("copyright", out var cpr))
        {
            var copyrights = cpr.GetProperty("items");
            copyoutputs = new ReleaseCopyright[copyrights.GetArrayLength()];
            using var arr = copyrights.EnumerateArray();
            int i = 0;
            while (arr.MoveNext())
            {
                var c = arr.Current;
                copyoutputs[i] = new ReleaseCopyright(
                    Type: c.GetProperty("type").GetString()!,
                    Text: c.GetProperty("text").GetString()!
                );
                i++;
            }
        }

        return new ArtistDiscographyRelease(
            Id: id,
            Name: name,
            Type: type,
            Copyright: copyoutputs,
            Date: date,
            Images: coverArts,
            Label: label,
            TotalTracks: tracksCount
        );
    }

    private static ArtistVisuals ParseArtistVisuals(JsonElement visuals)
    {
        var avatarImage = ParseCoverArt(visuals.GetProperty("avatarImage").GetProperty("sources"));

        var headerImages =
            visuals.TryGetProperty("headerImage", out var headerImgProp) && headerImgProp.ValueKind is not JsonValueKind.Null
                ? ParseCoverArt(headerImgProp.GetProperty("sources"))
                    : Array.Empty<CoverImage>();
        return new ArtistVisuals(
            AvatarImages: avatarImage,
            HeaderImage: headerImages.HeadOrNone()
        );
    }

    private static ArtistOverviewProfile ParseArtistProfile(JsonElement profile)
    {
        var name = profile.GetProperty("name").GetString()!;
        var verified = profile.GetProperty("verified").GetBoolean();

        Option<ArtistOverviewPinnedItem> pinnedItem = Option<ArtistOverviewPinnedItem>.None;
        if (profile.TryGetProperty("pinnedItem", out var potentialPin) &&
            potentialPin.ValueKind is not JsonValueKind.Null)
        {
            var comment = potentialPin.GetProperty("comment").GetString();
            using var backgroundImage =
                potentialPin.GetProperty("backgroundImage").GetProperty("sources").EnumerateArray();
            backgroundImage.MoveNext();
            var backgroundImageUrl = backgroundImage.Current.GetProperty("url").GetString();

            var itemType = potentialPin.GetProperty("type").GetString() switch
            {
                "ALBUM" => AudioItemType.Album,
                _ => AudioItemType.Unknown
            };

            var item = potentialPin.GetProperty("item");
            var uri = SpotifyId.FromUri(item.GetProperty("uri").GetString().AsSpan());
            var itemName = item.GetProperty("name").GetString()!;
            CoverImage[] coverArt = Array.Empty<CoverImage>();
            if (item.TryGetProperty("coverArt", out var coverArtProp))
            {
                coverArt = ParseCoverArt(coverArtProp.GetProperty("sources"));
            }
            pinnedItem = new ArtistOverviewPinnedItem(
                Id: uri,
                Name: itemName,
                Images: coverArt,
                Type: itemType,
                Comment: !string.IsNullOrEmpty(comment)
                    ? comment
                    : Option<string>.None,
                BackgroundImage: !string.IsNullOrEmpty(backgroundImageUrl)
                    ? backgroundImageUrl
                    : Option<string>.None
            );
        }

        return new ArtistOverviewProfile(
            Name: name,
            Verified: verified,
            PinnedItem: pinnedItem
        );
    }

    private static CoverImage[] ParseCoverArt(JsonElement sources)
    {
        using var data = sources.EnumerateArray();
        var output = new CoverImage[sources.GetArrayLength()];
        int i = 0;
        while (data.MoveNext())
        {
            var dt = data.Current;
            var url = dt.GetProperty("url").GetString()!;
            Option<ushort> height = Option<ushort>.None;
            Option<ushort> width = Option<ushort>.None;
            if (dt.TryGetProperty("height", out var potHeight) && potHeight.ValueKind is not JsonValueKind.Null)
            {
                height = potHeight.GetUInt16();
            }

            if (dt.TryGetProperty("width", out var potWidth) && potWidth.ValueKind is not JsonValueKind.Null)
            {
                width = potWidth.GetUInt16();
            }

            output[i] = new CoverImage(
                Url: url,
                Width: width,
                Height: height
            );
            i++;
        }

        return output;
    }
}

public readonly record struct ArtistTopTrack(SpotifyId Id, string Uid, string Name, Option<ulong> Playcount,
    int DiscNumber, TimeSpan Duration, ContentRatingType ContentRating, TrackArtist[] Artists, TrackAlbum Album);

public enum ContentRatingType
{
    NOTHING,
    NINETEEN_PLUS,
    EXPLICIT,
    UNKNOWN
}

public readonly record struct TrackAlbum(SpotifyId Id, CoverImage[] Images);

public interface ITrackArtist
{
    SpotifyId Id { get; }
    string Name { get; }
}

public readonly record struct TrackArtist(SpotifyId Id, string Name) : ITrackArtist;

public readonly record struct ArtistDiscographyRelease(SpotifyId Id, string Name, ReleaseType Type, ReleaseCopyright[] Copyright, DiscographyReleaseDate Date, CoverImage[] Images, Option<string> Label, ushort TotalTracks);

public readonly record struct DiscographyReleaseDate(DateTime Date, ReleaseDatePrecisionType Precision);

public enum ReleaseDatePrecisionType
{
    Unknown = 0,
    Year = 1,
    Month = 2,
    Day = 3
}

public readonly record struct ReleaseCopyright(string Type, string Text);

public enum ReleaseType
{
    Album,
    Single,
    Compilation
}

public readonly record struct ArtistOverviewPinnedItem(SpotifyId Id, string Name, CoverImage[] Images,
    AudioItemType Type, Option<string> Comment, Option<string> BackgroundImage);

public readonly record struct ArtistOverviewProfile(string Name, bool Verified,
    Option<ArtistOverviewPinnedItem> PinnedItem);

public readonly record struct ArtistVisuals(CoverImage[] AvatarImages, Option<CoverImage> HeaderImage);

public readonly record struct ArtistDiscography(Option<ArtistDiscographyRelease> LatestRelease,
    Option<ArtistDiscographyRelease>[] Albums, Option<ArtistDiscographyRelease>[] Singles,
    Option<ArtistDiscographyRelease>[] Compilations, ArtistTopTrack[] TopTracks);

public readonly record struct ArtistStats(ulong Followers, ulong MonthlyListeners, Option<ushort> WorldRank);

public readonly record struct ArtistRelatedContent;

public readonly record struct ArtistGoods;