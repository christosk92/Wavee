using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LanguageExt;
using Org.BouncyCastle.Math;
using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Models.Response;

internal readonly partial record struct SpotifyTrackAlbum(AudioId Id, string Name, Seq<Artwork> Artwork,
    DateOnly ReleaseDate, ReleaseDatePrecisionType ReleaseDatePrecision, int DiscNumber,
    string ArtistName) : ITrackAlbum, IComparable<SpotifyTrackAlbum>
{
    public static SpotifyTrackAlbum From(string cdnUrl, Show show)
    {
        return new SpotifyTrackAlbum(
            Id: AudioId.FromRaw(show.Gid.Span, AudioItemType.PodcastShow, ServiceType.Spotify),
            Name: show.Name,
            Artwork: show.CoverImage?.Image?.Select(x => ToArtwork(cdnUrl, x))?.ToSeq() ?? LanguageExt.Seq<Artwork>.Empty,
            ReleaseDate: new DateOnly(1, 1, 1),
            ReleaseDatePrecision: ReleaseDatePrecisionType.Unknown,
            DiscNumber: 0,
            ArtistName: show.Publisher
        );
    }
    public static SpotifyTrackAlbum From(string cdnUrl, Album album, int discNumber)
    {
        if (album is null) return default;
        var (releaseDate, precision) = ParseReleaseDate(album.Date);


        return new SpotifyTrackAlbum(
            Id: AudioId.FromRaw(album.Gid.Span, AudioItemType.Album, ServiceType.Spotify),
            Name: album.Name,
            Artwork: album.CoverGroup?.Image?.Select(x => ToArtwork(cdnUrl, x))?.ToSeq() ?? LanguageExt.Seq<Artwork>.Empty,
            ReleaseDate: releaseDate,
            ReleaseDatePrecision: precision,
            DiscNumber: discNumber,
            ArtistName: album.Artist[0].Name
        );
    }

    private static (DateOnly ParseReleaseDate, ReleaseDatePrecisionType Precision)
        ParseReleaseDate(Date albumDate)
    {
        if (albumDate is null) return (new DateOnly(1, 1, 1), ReleaseDatePrecisionType.Unknown);
        if (albumDate.HasDay)
        {
            //it is safe to assume that if the album has a day, it also has a month and a year
            return (new DateOnly(albumDate.Year, albumDate.Month, albumDate.Day), ReleaseDatePrecisionType.Day);
        }

        if (albumDate.HasMonth)
        {
            //it is safe to assume that if the album has a month, it also has a year
            return (new DateOnly(albumDate.Year, albumDate.Month, 1), ReleaseDatePrecisionType.Month);
        }

        if (albumDate.HasYear)
        {
            return (new DateOnly(albumDate.Year, 1, 1), ReleaseDatePrecisionType.Year);
        }

        return (new DateOnly(1, 1, 1), ReleaseDatePrecisionType.Unknown);
    }

    private static Artwork ToArtwork(string cdnUrl, Image image)
    {
        cdnUrl ??= "https://i.scdn.co/image/{file_id}";
        var imageId = ToBase16(image.FileId.Span);
        return new Artwork(
            Url: cdnUrl.Replace("{file_id}", imageId),
            Width: image.HasWidth ? image.Width : None,
            Height: image.HasHeight ? image.Height : None,
            Size: image.HasSize ? ToSize(image.Size) : None
        );
    }

    private static ArtworkSizeType ToSize(Image.Types.Size imageSize)
    {
        return imageSize switch
        {
            Image.Types.Size.Default => ArtworkSizeType.Default,
            Image.Types.Size.Small => ArtworkSizeType.Small,
            Image.Types.Size.Large => ArtworkSizeType.Large,
            Image.Types.Size.Xlarge => ArtworkSizeType.ExtraLarge,
            _ => throw new ArgumentOutOfRangeException(nameof(imageSize), imageSize, null)
        };
    }

    private static string ToBase16(ReadOnlySpan<byte> bytes)
    {
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    public int CompareTo(SpotifyTrackAlbum tr) => CompareTo((ITrackAlbum)tr);

    public int CompareTo(ITrackAlbum other)
    {
        /*
        #0055b7
        19FACT
        2006
        20/20
        ..29 2012 Repackaged version
        The 3rd EP
        5296
        5.3 (Gradation)
        Affirmation
        AM 5:00
        American Idiot
        An-nyeong
        Answer
        Any song
        Arashi No.1 (Ichigou)
        BABEL
        Backstreet's Back  
         */
        //for some reason, spotify uses a very weird sorting algorithm for album names
        //this is an attempt to replicate it
        var thisName = Name;
        var otherName = other.Name;


        //question: why does The 3rd EP come before 5296?
        //Its weird because 'A' is obviously before 'T' in the alphabet
        //so why does 'T' come before 'A'?

        /*
         * Writers and companies typically follow editorial standards/guidelines like APA style, which has a rule to ignore insignificant words like “a”, “an”, and “the” when alphabetizing a list of terms or references.
           
           So for “the 3rd EP,” the 3 is considered the first character in the name. I can’t see the one with quotation marks that you mentioned, but I assume whatever character comes after the first quotation mark is determining it’s placement.
           
           Overall, it appears they use the following order:
           
           1) non-alphanumeric characters (excluding punctuation marks)
           
           2) digits and numerals
           
           3) letters A-Z
         */

        //remove insignificant words (ONLY LEADING)
        thisName = InsignificantWords().Replace(thisName, string.Empty);
        otherName = InsignificantWords().Replace(otherName, string.Empty);

        //remove punctuation
        thisName = PunctuationRegex().Replace(thisName, string.Empty);
        otherName = PunctuationRegex().Replace(otherName, string.Empty);

        return string.Compare(thisName, otherName, CultureInfo.InvariantCulture,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols);
    }


    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is ITrackAlbum other) return CompareTo(other);
        throw new ArgumentException($"Object must be of type {nameof(ITrackAlbum)}");
    }

    [GeneratedRegex(@"^(the |an |a )", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex InsignificantWords();

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex PunctuationRegex();
}