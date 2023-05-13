using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Id;
using Wavee.Spotify.Infrastructure;

namespace Wavee.Spotify.Models.Responses;

internal readonly record struct SpotifyTrackAlbum(AudioId Id, string Name, Seq<Artwork> Artwork,
    DateOnly ReleaseDate, ReleaseDatePrecisionType ReleaseDatePrecision) : ITrackAlbum
{
    public static SpotifyTrackAlbum From(string cdnUrl, Album album)
    {
        var (releaseDate, precision) = ParseReleaseDate(album.Date);
        return new SpotifyTrackAlbum(
            Id: album.ToId(),
            Name: album.Name,
            Artwork: album.CoverGroup.Image.Select(x => ToArtwork(cdnUrl, x)).ToSeq(),
            ReleaseDate: releaseDate,
            ReleaseDatePrecision: precision
        );
    }

    private static (DateOnly ParseReleaseDate, ReleaseDatePrecisionType Precision)
        ParseReleaseDate(Date albumDate)
    {
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
        var imageId = image.ToBase16();
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
}