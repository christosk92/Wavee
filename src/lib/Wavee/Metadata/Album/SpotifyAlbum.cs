using LanguageExt;
using Spotify.Metadata;
using System;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Wavee.Id;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;

namespace Wavee.Metadata.Album;

public sealed class SpotifyAlbum
{
    public SpotifyAlbum(SpotifyId id, string name, AlbumArtist[] artists, DateTime releaseDate,
        ReleaseDatePrecisionType releaseDatePrecision, CoverImage[] images, Option<SpotifyColors> colors,
        SpotifyAlbumDisc[] discs, SpotifyAlbum[] moreAlbums, string[] copyrights)
    {
        Id = id;
        Name = name;
        Artists = artists;
        ReleaseDate = releaseDate;
        ReleaseDatePrecision = releaseDatePrecision;
        Images = images;
        Colors = colors;
        Discs = discs;
        MoreAlbums = moreAlbums;
        Copyrights = copyrights;
    }
    public SpotifyId Id { get; }
    public string Name { get; }
    public AlbumArtist[] Artists { get; }
    public DateTime ReleaseDate { get; }
    public ReleaseDatePrecisionType ReleaseDatePrecision { get; }
    public CoverImage[] Images { get; }
    public Option<SpotifyColors> Colors { get; }
    public SpotifyAlbumDisc[] Discs { get; }
    public SpotifyAlbum[] MoreAlbums { get; }
    public string[] Copyrights { get; }

    internal static SpotifyAlbum ParseFrom(JsonElement root)
    {
        var name = root.GetProperty("name").GetString();
        var coverArt = ParseImages(root.GetProperty("coverArt"));

        static SpotifyAlbumDisc[] GetDiscs(JsonElement root)
        {
            if (!root.TryGetProperty("tracks", out var tracksArrda))
                return Array.Empty<SpotifyAlbumDisc>();
            using var allTracks = tracksArrda.GetProperty("items").EnumerateArray();

            var discs = root.GetProperty("discs");
            var totalDiscs = discs.GetProperty("totalCount").GetUInt16();
            var discList = new SpotifyAlbumDisc[totalDiscs];

            int disc_i = 0;
            using var discArr = discs.GetProperty("items").EnumerateArray();
            while (discArr.MoveNext())
            {
                var disc = discArr.Current;
                var number = disc.GetProperty("number").GetUInt16();
                var tracksCount = disc.GetProperty("tracks").GetProperty("totalCount").GetUInt16();
                var tracks = new SpotifyAlbumTrack[tracksCount];
                int j = 0;
                while (j < tracksCount && allTracks.MoveNext())
                {
                    var track = allTracks.Current;
                    var uid = track.GetProperty("uid").GetString();
                    var actualTrack = track.GetProperty("track");
                    //TODO:
                    tracks[j++] = new SpotifyAlbumTrack(
                        uid: uid!,
                        id: SpotifyId.FromUri(actualTrack.GetProperty("uri").GetString().AsSpan()),
                        name: actualTrack.GetProperty("name").GetString()!,
                        duration: TimeSpan.FromMilliseconds(actualTrack.GetProperty("duration").GetProperty("totalMilliseconds").GetUInt32()!),
                        trackNumber: actualTrack.GetProperty("trackNumber").GetUInt16()!,
                        discNumber: actualTrack.GetProperty("discNumber").GetUInt16()!,
                        contentRating: ParseContentRating(actualTrack.GetProperty("contentRating").GetProperty("label").GetString()!),
                        artists: ParseArtists(actualTrack.GetProperty("artists")),
                        playcount: ParsePlaycount(actualTrack),
                        saved: actualTrack.GetProperty("saved").GetBoolean()
                    );
                }

                discList[disc_i++] = new SpotifyAlbumDisc(
                    number: number,
                    tracks: tracks
                );
            }

            return discList;
        }

        var discs = GetDiscs(root);

        DateTime releaseDateValue = default;
        ReleaseDatePrecisionType precision = ReleaseDatePrecisionType.Unknown;
        if (root.TryGetProperty("date", out var rld))
        {
            if (rld.TryGetProperty("precision", out var prec))
            {
                var releaseDatePrecision = prec.GetString();
                var isoString = rld.GetProperty("isoString").GetString();
                releaseDateValue = DateTime.Parse(isoString!);
                precision = releaseDatePrecision switch
                {
                    "YEAR" => ReleaseDatePrecisionType.Year,
                    "MONTH" => ReleaseDatePrecisionType.Month,
                    "DAY" => ReleaseDatePrecisionType.Day,
                    _ => throw new Exception("Invalid precision")
                };
            }
            else if (rld.TryGetProperty("year", out var year))
            {
                releaseDateValue = new DateTime(year: year.GetUInt16(), month: 1, day: 1);
                precision = ReleaseDatePrecisionType.Year;
            }
        }

        static SpotifyAlbum[] ParseMoreAlbums(JsonElement root)
        {
            if (root.TryGetProperty("moreAlbumsByArtist", out var moreAlbumsRoot))
            {
                var rootDiscography = moreAlbumsRoot.GetProperty("items");
                if (rootDiscography.GetArrayLength() > 0)
                {
                    var moreAlbums = rootDiscography[0].GetProperty("discography").GetProperty("popularReleasesAlbums").GetProperty("items");
                    var moreAlbumsOutput = new SpotifyAlbum[moreAlbums.GetArrayLength()];
                    int i = 0;
                    using var arr = moreAlbums.EnumerateArray();
                    while (arr.MoveNext())
                    {
                        var moreAlbum = arr.Current;
                        moreAlbumsOutput[i] = ParseFrom(moreAlbum);
                        i++;
                    }

                    return moreAlbumsOutput;
                }
            }

            return Array.Empty<SpotifyAlbum>();
        }
        var moreAlbums = ParseMoreAlbums(root);
        var artists = ParseAlbumArtists(root);
        var copyrights = ParseCopyrights(root);

        return new(
            id: SpotifyId.FromUri(root.GetProperty("uri").GetString().AsSpan()),
            name: name!,
            artists: artists,
            releaseDate: releaseDateValue,
            releaseDatePrecision: precision,
            images: coverArt.Images,
            colors: coverArt.Colors,
            discs: discs,
            moreAlbums: moreAlbums,
            copyrights: copyrights
        );
    }

    private static string[] ParseCopyrights(JsonElement root)
    {
        if (root.TryGetProperty("copyright", out var coypright))
        {
            var items = coypright.GetProperty("items");
            var output = new string[items.GetArrayLength()];
            int i = 0;
            using var arr = items.EnumerateArray();
            while (arr.MoveNext())
            {
                var current = arr.Current;
                output[i] = current.GetProperty("text").GetString()!;
                i++;
            }

            return output;
        }

        return Array.Empty<string>();
    }

    private static AlbumArtist[] ParseAlbumArtists(JsonElement root)
    {
        if (root.TryGetProperty("artists", out var artists))
        {
            var items = artists.GetProperty("items");
            var output = new AlbumArtist[items.GetArrayLength()];
            int i = 0;
            using var arr = items.EnumerateArray();
            while (arr.MoveNext())
            {
                var current = arr.Current;
                var name = current.GetProperty("profile").GetProperty("name").GetString();
                var image = ParseImagesOnly(
                    current.GetProperty("visuals").GetProperty("avatarImage").GetProperty("sources"));
                output[i] = new AlbumArtist(
                    Id: SpotifyId.FromUri(current.GetProperty("uri").GetString().AsSpan()),
                    Name: name!,
                    Images: image
                );
            }

            return output;
        }

        return Array.Empty<AlbumArtist>();
    }

    private static Option<ulong> ParsePlaycount(JsonElement track)
    {
        Option<ulong> playcount = Option<ulong>.None;
        if (track.TryGetProperty("playcount", out var potplc) && potplc.ValueKind is JsonValueKind.String)
        {
            var val = potplc.GetString();
            if (ulong.TryParse(val, out var playcoun))
            {
                playcount = playcoun;
            }
        }

        return playcount;
    }

    private static TrackArtist[] ParseArtists(JsonElement getProperty)
    {
        var artists = getProperty.GetProperty("items");
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

        return artistsOutput;
    }

    private static ContentRatingType ParseContentRating(string label)
    {
        return label switch
        {
            "NONE" => ContentRatingType.NOTHING,
            "NINETEEN_PLUS" => ContentRatingType.NINETEEN_PLUS,
            "EXPLICIT" => ContentRatingType.EXPLICIT,
            _ => ContentRatingType.UNKNOWN
        };
    }

    private static (CoverImage[] Images, Option<SpotifyColors> Colors) ParseImages(JsonElement coverArt)
    {
        if (coverArt.TryGetProperty("extractedColors", out var cls))
        {
            var colors = new SpotifyColors(
                cls.GetProperty("colorRaw").GetProperty("hex").GetString()!,
                cls.GetProperty("colorLight").GetProperty("hex").GetString()!,
                cls.GetProperty("colorDark").GetProperty("hex").GetString()!
            );
            var sources = coverArt.GetProperty("sources");
            var images = ParseImagesOnly(sources);
            return (images, colors);
        }

        return (ParseImagesOnly(coverArt.GetProperty("sources")), Option<SpotifyColors>.None);
    }

    private static CoverImage[] ParseImagesOnly(JsonElement sources)
    {
        var images = new CoverImage[sources.GetArrayLength()];
        int i = 0;
        using var arr = sources.EnumerateArray();
        while (arr.MoveNext())
        {
            var img = arr.Current;
            var url = img.GetProperty("url").GetString()!;
            var potentialWidth = img.TryGetProperty("width", out var wd)
                                 && wd.ValueKind is JsonValueKind.Number
                ? wd.GetUInt16()
                : Option<ushort>.None;
            var potentialHeight = img.TryGetProperty("height", out var ht)
                                  && ht.ValueKind is JsonValueKind.Number
                ? ht.GetUInt16()
                : Option<ushort>.None;
            images[i++] = new CoverImage(
                Url: url,
                Width: potentialWidth,
                Height: potentialHeight
            );
        }

        return images;
    }
}

public readonly record struct AlbumArtist(SpotifyId Id, string Name, CoverImage[] Images);

public sealed class SpotifyAlbumDisc
{
    public SpotifyAlbumDisc(ushort number, SpotifyAlbumTrack[] tracks)
    {
        Number = number;
        Tracks = tracks;
    }

    public ushort Number { get; }
    public SpotifyAlbumTrack[] Tracks { get; }
}

public sealed class SpotifyAlbumTrack
{
    public SpotifyAlbumTrack(string uid, SpotifyId id, string name, TimeSpan duration, ushort trackNumber, ushort discNumber, ContentRatingType contentRating, TrackArtist[] artists, Option<ulong> playcount, bool saved)
    {
        Uid = uid;
        Id = id;
        Name = name;
        Duration = duration;
        TrackNumber = trackNumber;
        DiscNumber = discNumber;
        ContentRating = contentRating;
        Artists = artists;
        Playcount = playcount;
        Saved = saved;
    }
    public string Uid { get; }
    public SpotifyId Id { get; }
    public string Name { get; }
    public TimeSpan Duration { get; }
    public ushort TrackNumber { get; }
    public ushort DiscNumber { get; }
    public ContentRatingType ContentRating { get; }

    public TrackArtist[] Artists { get; }
    public Option<ulong> Playcount { get; }
    public bool Saved { get; }
}

public readonly record struct SpotifyColors(string ColorRaw, string ColorLight, string ColorDark);
