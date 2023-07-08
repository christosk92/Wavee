using System;
using System.Text.Json;
using Eum.Spotify.playlist4;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Metadata.Common;
using Wavee.Metadata.Home;

namespace Wavee.Metadata.Artist;

public readonly record struct ArtistOverview(SpotifyId Id, bool IsSaved,
    Option<IArtistPreReleaseItem> PreRelease,
    Uri SharingUrl,
    ArtistOverviewProfile Profile,
    ArtistVisuals Visuals,
    ArtistDiscography Discography,
    ArtistStats Statistics,
    ArtistGoods Goods)
{
    internal static ArtistOverview ParseFrom(ReadOnlyMemory<byte> data)
    {
        using var jsonDocument = JsonDocument.Parse(data);
        var artist = jsonDocument.RootElement.GetProperty("data").GetProperty("artistUnion");
        var uri = SpotifyId.FromUri(artist.GetProperty("uri").GetString().AsSpan());
        var saved = artist.GetProperty("saved").GetBoolean();
        var sharingUrl = artist.GetProperty("sharingInfo").GetProperty("shareUrl").GetString()!;
        Option<IArtistPreReleaseItem> prerelease = Option<IArtistPreReleaseItem>.None;
        if (artist.TryGetProperty("preRelease", out var prl)
            && prl.ValueKind is not JsonValueKind.Null)
        {
            var preReleaseUri = SpotifyId.FromUri(prl.GetProperty("uri").GetString().AsSpan());
            var releaseDate = prl.GetProperty("releaseDate").GetProperty("isoString").GetDateTimeOffset();
            var preReleaseContent = prl.GetProperty("preReleaseContent");
            var name = preReleaseContent.GetProperty("name").GetString()!;
            var type = preReleaseContent.GetProperty("type").GetString()!;
            var preReleaseCoverArt = ParseCoverArt(preReleaseContent.GetProperty("coverArt").GetProperty("sources"));

            prerelease = new SpotifyPreReleaseItem(
                Id: preReleaseUri,
                Name: name,
                Type: type switch
                {
                    "ALBUM" => ReleaseType.Album,
                    "EP" or "SINGLE" => ReleaseType.Single,
                    "COMPILATION" => ReleaseType.Compilation,
                    _ => ReleaseType.Album
                },
                ReleaseDate: releaseDate,
                Images: preReleaseCoverArt
            );
        }

        var relatedContent = artist.GetProperty("relatedContent");
        var profile = ParseArtistProfile(artist.GetProperty("profile"), relatedContent);
        var visuals = ParseArtistVisuals(artist.GetProperty("visuals"));
        var discography = ParseArtistDiscography(artist.GetProperty("discography"), relatedContent);
        var stats = ParseArtistStats(artist.GetProperty("stats"));
        var goods = ParseArtistGoods(artist.GetProperty("goods"));

        return new ArtistOverview(
            Id: uri,
            IsSaved: saved,
            PreRelease: prerelease,
            SharingUrl: new Uri(sharingUrl),
            Profile: profile,
            Visuals: visuals,
            Discography: discography,
            Statistics: stats,
            Goods: goods
        );
    }


    private static ArtistGoods ParseArtistGoods(JsonElement getProperty)
    {
        return new ArtistGoods();
    }

    private static ArtistStats ParseArtistStats(JsonElement stats)
    {
        var followers = stats.GetProperty("followers").GetUInt64();
        var monthlyListeners = stats.GetProperty("monthlyListeners").GetUInt64();
        var worldRank = stats.TryGetProperty("worldRank", out var worldRankProp)
                        && worldRankProp.ValueKind is not JsonValueKind.Null
            ? worldRankProp.GetUInt16()
            : (ushort)0;
        return new ArtistStats(
            Followers: followers,
            MonthlyListeners: monthlyListeners,
            WorldRank: worldRank is 0 ? Option<ushort>.None : worldRank
        );
    }

    private static ArtistDiscography ParseArtistDiscography(JsonElement discography, JsonElement relatedContent)
    {
        Option<IArtistDiscographyRelease> latestRelease = Option<IArtistDiscographyRelease>.None;
        if (discography.TryGetProperty("latest", out var potentialLatest) &&
            potentialLatest.ValueKind is not JsonValueKind.Null)
        {
            latestRelease = ParseRelease(potentialLatest);
        }

        var albums = ParseDiscographyGroup(discography.GetProperty("albums"));
        var singles = ParseDiscographyGroup(discography.GetProperty("singles"));
        var compilations = ParseDiscographyGroup(discography.GetProperty("compilations"));
        var topTracks = ParseTopTracks(discography.GetProperty("topTracks").GetProperty("items"));
        var appearsOn = ParseAppearsOn(relatedContent.GetProperty("appearsOn"));
        return new ArtistDiscography(
            LatestRelease: latestRelease,
            Albums: albums,
            Singles: singles,
            Compilations: compilations,
            TopTracks: topTracks,
            AppearsOn: appearsOn
        );
    }

    private static Option<SpotifyAlbumHomeItem>[] ParseAppearsOn(JsonElement getProperty)
    {
        var totalCount = getProperty.GetProperty("totalCount").GetUInt16();
        var output = new Option<SpotifyAlbumHomeItem>[totalCount];
        using var items = getProperty.GetProperty("items").EnumerateArray();
        int i = -1;
        for (int x = 0; x < totalCount; x++)
        {
            i++;
            if (!items.MoveNext())
            {
                output[i] = Option<SpotifyAlbumHomeItem>.None;
                continue;
            }

            var current = items.Current;
            //nested releases, get first
            using var releases = current.GetProperty("releases").GetProperty("items").EnumerateArray();
            releases.MoveNext();

            var item = SpotifyItemParser.ParseFrom(releases.Current);
            if (item.IsSome && item.ValueUnsafe() is SpotifyAlbumHomeItem pl)
            {
                output[i] = pl;
            }
            else
            {
                output[i] = Option<SpotifyAlbumHomeItem>.None;
            }
        }

        return output;
    }

    private static Option<IArtistDiscographyRelease>[] ParseDiscographyGroup(JsonElement group)
    {
        var totalCount = group.GetProperty("totalCount").GetUInt16();
        var output = new Option<IArtistDiscographyRelease>[totalCount];
        for (int k = 0; k < totalCount; k++)
        {
            output[k] = Option<IArtistDiscographyRelease>.None;
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
            output[i] = Option<IArtistDiscographyRelease>.Some(parsedRelease);

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
                Id: album.GetProperty("uri").GetString()!,
                Name: string.Empty, 
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
            "COMPILATION" => ReleaseType.Compilation,
            _ => throw new ArgumentOutOfRangeException()
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

        var copyoutputs = Array.Empty<IReleaseCopyright>();

        if (release.TryGetProperty("copyright", out var cpr))
        {
            var copyrights = cpr.GetProperty("items");
            copyoutputs = new IReleaseCopyright[copyrights.GetArrayLength()];
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
                    : Array.Empty<ICoverImage>();
        return new ArtistVisuals(
            AvatarImages: avatarImage,
            HeaderImage: headerImages.HeadOrNone()
        );
    }

    private static ArtistOverviewProfile ParseArtistProfile(JsonElement profile, JsonElement relatedContent)
    {
        var name = profile.GetProperty("name").GetString()!;
        var verified = profile.GetProperty("verified").GetBoolean();

        Option<IArtistOverviewPinnedItem> pinnedItem = Option<IArtistOverviewPinnedItem>.None;
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
                "CONCERT" => AudioItemType.Concert,
                _ => AudioItemType.Unknown
            };
            var item = potentialPin.GetProperty("item");

            if (itemType is AudioItemType.Concert)
            {
                var concertId = item.GetProperty("id").GetString();
                var title = item.GetProperty("title").GetString();
                var date = item.GetProperty("date").GetProperty("isoString").GetDateTimeOffset();
                var venue = item.GetProperty("venue");
                var venueName = venue.GetProperty("name").GetString();
                var venueLocaiton = venue.GetProperty("location").GetProperty("name").GetString();
                pinnedItem = new ArtistOverviewPinnedConcert(
                    ConcertId: concertId!,
                    Name: title!,
                    Type: itemType,
                    Date: date,
                    Venue: new ConcertVenueDetails(Name: venueName!, Location: venueLocaiton!),
                    Comment: !string.IsNullOrEmpty(comment)
                        ? comment
                        : Option<string>.None,
                    BackgroundImage: !string.IsNullOrEmpty(backgroundImageUrl)
                        ? backgroundImageUrl
                        : Option<string>.None
                );
            }
            else
            {
                var uri = SpotifyId.FromUri(item.GetProperty("uri").GetString().AsSpan());
                var itemName = item.GetProperty("name").GetString()!;
                ICoverImage[] coverArt = Array.Empty<ICoverImage>();
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
        }

        var playlists = profile.GetProperty("playlistsV2");
        var totalCount = playlists.GetProperty("totalCount").GetUInt16();
        using var playlistData = playlists.GetProperty("items").EnumerateArray();
        var playlistsOutput = new Option<SpotifyPlaylistHomeItem>[totalCount];
        int i = -1;
        for (int x = 0; x < totalCount; x++)
        {
            i++;
            if (!playlistData.MoveNext())
            {
                playlistsOutput[i] = Option<SpotifyPlaylistHomeItem>.None;
                continue;
            }

            var parsedItem = SpotifyItemParser.ParseFrom(playlistData.Current.GetProperty("data"));
            if (parsedItem.IsSome && parsedItem.ValueUnsafe() is SpotifyPlaylistHomeItem playlist)
            {
                playlistsOutput[i] = playlist;
            }
        }


        var relatedArtists = relatedContent.GetProperty("relatedArtists");
        var relatedArtistsCount = relatedArtists.GetProperty("totalCount").GetUInt16();
        using var relatedArtistsData = relatedArtists.GetProperty("items").EnumerateArray();
        var related = new Option<SpotifyArtistHomeItem>[relatedArtistsCount];
        int j = -1;

        for (int x = 0; x < relatedArtistsCount; x++)
        {
            j++;
            if (!relatedArtistsData.MoveNext())
            {
                related[j] = Option<SpotifyArtistHomeItem>.None;
                continue;
            }

            var parsedItem = SpotifyItemParser.ParseFrom(relatedArtistsData.Current);
            if (parsedItem.IsSome && parsedItem.ValueUnsafe() is SpotifyArtistHomeItem artist)
            {
                related[j] = artist;
            }
            else
            {
                related[j] = Option<SpotifyArtistHomeItem>.None;
            }
        }


        var discoveredOn = relatedContent.GetProperty("discoveredOnV2");
        var discoveredOnCount = discoveredOn.GetProperty("totalCount").GetUInt16();
        using var discoveredOnData = discoveredOn.GetProperty("items").EnumerateArray();
        var discoveredOnOutput = new Option<SpotifyPlaylistHomeItem>[discoveredOnCount];
        int k = -1;
        for (int x = 0; x < discoveredOnCount; x++)
        {
            k++;
            if (!discoveredOnData.MoveNext())
            {
                discoveredOnOutput[k] = Option<SpotifyPlaylistHomeItem>.None;
                continue;
            }

            var parsedItem = SpotifyItemParser.ParseFrom(discoveredOnData.Current.GetProperty("data"));
            if (parsedItem.IsSome && parsedItem.ValueUnsafe() is SpotifyPlaylistHomeItem playlist)
            {
                discoveredOnOutput[k] = playlist;
            }
            else
            {
                discoveredOnOutput[k] = Option<SpotifyPlaylistHomeItem>.None;
            }
        }

        return new ArtistOverviewProfile(
            Name: name,
            Verified: verified,
            PinnedItem: pinnedItem,
            Related: related,
            Playlists: playlistsOutput,
            DiscoveredOn: discoveredOnOutput
        );
    }

    private static ICoverImage[] ParseCoverArt(JsonElement sources)
    {
        using var data = sources.EnumerateArray();
        var output = new ICoverImage[sources.GetArrayLength()];
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

public readonly record struct SpotifyPreReleaseItem(SpotifyId Id, ReleaseType Type, DateTimeOffset ReleaseDate,
        string Name, ICoverImage[] Images)
    : IArtistPreReleaseItem;

public interface IArtistPreReleaseItem
{
    SpotifyId Id { get; }
    ReleaseType Type { get; }
    DateTimeOffset ReleaseDate { get; }
    string Name { get; }
    ICoverImage[] Images { get; }
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

public readonly record struct TrackAlbum(string Id, string Name, ICoverImage[] Images) : ITrackAlbum;

public interface ITrackAlbum
{
    string Id { get; }
    string Name { get; }
    ICoverImage[] Images { get; }
}
public interface ITrackArtist
{
    SpotifyId Id { get; }
    string Name { get; }
}

public readonly record struct TrackArtist(SpotifyId Id, string Name) : ITrackArtist;

public readonly record struct ArtistDiscographyRelease(SpotifyId Id, string Name, ReleaseType Type,
        IReleaseCopyright[] Copyright, DiscographyReleaseDate Date, ICoverImage[] Images, Option<string> Label,
        ushort TotalTracks)
    : IArtistDiscographyRelease;

public interface IArtistDiscographyRelease
{
    SpotifyId Id { get; }
    string Name { get; }
    ReleaseType Type { get; }
    IReleaseCopyright[] Copyright { get; }
    DiscographyReleaseDate Date { get; }
    ICoverImage[] Images { get; }
    Option<string> Label { get; }
    ushort TotalTracks { get; }
}

public readonly record struct DiscographyReleaseDate(DateTime Date, ReleaseDatePrecisionType Precision);

public enum ReleaseDatePrecisionType
{
    Unknown = 0,
    Year = 1,
    Month = 2,
    Day = 3
}

public readonly record struct ReleaseCopyright(string Type, string Text) : IReleaseCopyright;

public interface IReleaseCopyright
{
    string Type { get; }
    string Text { get; }
}
public enum ReleaseType
{
    Album,
    Single,
    Compilation
}

public readonly record struct ArtistOverviewPinnedItem(SpotifyId Id, string Name, ICoverImage[] Images,
    AudioItemType Type, Option<string> Comment, Option<string> BackgroundImage) : IArtistOverviewPinnedItem;

public readonly record struct ArtistOverviewPinnedConcert(string ConcertId, string Name, DateTimeOffset Date,
    ConcertVenueDetails Venue,
    AudioItemType Type,
    Option<string> Comment, Option<string> BackgroundImage) : IArtistOverviewPinnedItem;

public readonly record struct ConcertVenueDetails(string Name, string Location);
public interface IArtistOverviewPinnedItem
{
    AudioItemType Type { get; }
    string Name { get; }
    Option<string> Comment { get; }
    Option<string> BackgroundImage { get; }
}
public readonly record struct ArtistOverviewProfile(string Name, bool Verified,
    Option<IArtistOverviewPinnedItem> PinnedItem,
    Option<SpotifyArtistHomeItem>[] Related,
    Option<SpotifyPlaylistHomeItem>[] Playlists,
    Option<SpotifyPlaylistHomeItem>[] DiscoveredOn
    );

public readonly record struct ArtistVisuals(ICoverImage[] AvatarImages, Option<ICoverImage> HeaderImage);

public readonly record struct ArtistDiscography(Option<IArtistDiscographyRelease> LatestRelease,
    Option<IArtistDiscographyRelease>[] Albums,
    Option<IArtistDiscographyRelease>[] Singles,
    Option<IArtistDiscographyRelease>[] Compilations,
    Option<SpotifyAlbumHomeItem>[] AppearsOn,
    ArtistTopTrack[] TopTracks);

public readonly record struct ArtistStats(ulong Followers, ulong MonthlyListeners, Option<ushort> WorldRank);


public readonly record struct ArtistGoods;