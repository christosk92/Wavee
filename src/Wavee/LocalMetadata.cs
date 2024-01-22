using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using TagLib;
using static LanguageExt.Prelude;
using File = System.IO.File;


namespace Wavee;

public static class LocalMetadata
{
    public static void ReplaceMetadata(string filePath, LocalTrackMetadata fillTrackMetadata)
    {
        using var tagFile = TagLib.File.Create(filePath);
        fillTrackMetadata.Title.IfSome(x => tagFile.Tag.Title = x);
        fillTrackMetadata.Composers.IfSome(x =>
        {
            tagFile.Tag.Composers = x.ToArray();
            tagFile.Tag.Performers = x.ToArray();
        });
        fillTrackMetadata.AlbumArtists.IfSome(x => tagFile.Tag.AlbumArtists = x.ToArray());
        fillTrackMetadata.Album.IfSome(x => tagFile.Tag.Album = x);
        fillTrackMetadata.Genres.IfSome(x => tagFile.Tag.Genres = x.ToArray());
        fillTrackMetadata.Comment.IfSome(x => tagFile.Tag.Comment = x);
        fillTrackMetadata.TrackNumber.IfSome(x => tagFile.Tag.Track = (uint)x);
        fillTrackMetadata.Year.IfSome(x => tagFile.Tag.Year = (uint)x);
        fillTrackMetadata.DiscNumber.IfSome(x => tagFile.Tag.Disc = (uint)x);

        if (fillTrackMetadata.GetImageBytes.IsSome)
        {
            var imageBytes = fillTrackMetadata.GetImageBytes.ValueUnsafe()();
            if (imageBytes.IsSome)
            {
                var picture = new TagLib.Picture
                {
                    Type = PictureType.FrontCover,
                    MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                    Description = "Cover",
                    Data = new ByteVector(imageBytes.ValueUnsafe())
                };
                tagFile.Tag.Pictures = new IPicture[] { picture };
            }
        }

        tagFile.Save();
    }

    public static LocalTrackMetadata GetMetadata(string filePath)
    {
        var tagFile = TagLib.File.Create(filePath);
        Option<string> title = Option<string>.None;
        if (!string.IsNullOrWhiteSpace(tagFile.Tag.Title))
        {
            title = tagFile.Tag.Title;
        }

        Option<string[]> composers = Option<string[]>.None;
        if (tagFile.Tag.Composers is not null)
        {
            composers = tagFile.Tag.Composers;
        }

        Option<string[]> albumArtists = Option<string[]>.None;
        if (tagFile.Tag.AlbumArtists is not null)
        {
            albumArtists = tagFile.Tag.AlbumArtists;
        }

        Option<string> album = Option<string>.None;
        if (!string.IsNullOrWhiteSpace(tagFile.Tag.Album))
        {
            album = tagFile.Tag.Album;
        }

        Option<string[]> genres = Option<string[]>.None;
        if (tagFile.Tag.Genres is not null)
        {
            genres = tagFile.Tag.Genres;
        }

        Option<string> comment = Option<string>.None;
        if (!string.IsNullOrWhiteSpace(tagFile.Tag.Comment))
        {
            comment = tagFile.Tag.Comment;
        }

        Option<int> trackNumber = Option<int>.None;
        if (tagFile.Tag.Track is not 0)
        {
            trackNumber = (int)tagFile.Tag.Track;
        }

        Option<int> year = Option<int>.None;
        if (tagFile.Tag.Year is not 0)
        {
            year = (int)tagFile.Tag.Year;
        }

        Option<int> discNumber = Option<int>.None;
        if (tagFile.Tag.Disc is not 0)
        {
            discNumber = (int)tagFile.Tag.Disc;
        }


        return new LocalTrackMetadata(
            filePath,
            title, composers, albumArtists, album, genres,
            comment, trackNumber, year, discNumber,
            Some(() => openImage(filePath)),
            Some(() => openImageBytes(filePath)));
    }

    private static Option<byte[]> openImageBytes(string filePath)
    {
        using var tagFile = TagLib.File.Create(filePath);
        if (tagFile.Tag.Pictures.Length > 0)
        {
            var bin = tagFile.Tag.Pictures[0].Data.Data;
            return bin;
        }

        return Option<byte[]>.None;
    }

    private static Option<string> openImage(string filePath) => openImageBytes(filePath)
        .Map(x =>
        {
            var temp = Path.GetTempFileName();
            File.WriteAllBytes(temp, x);
            return "file://" + temp;
        });
}

public readonly record struct LocalTrackMetadata
{
    public readonly string FilePath;
    public readonly Option<string> Title;
    public readonly Option<string[]> Composers;
    public readonly Option<string[]> AlbumArtists;
    public readonly Option<string> Album;
    public readonly Option<string[]> Genres;
    public readonly Option<string> Comment;
    public readonly Option<int> TrackNumber;
    public readonly Option<int> Year;
    public readonly Option<int> DiscNumber;
    public readonly Option<Func<Option<string>>> GetImageFile;
    public readonly Option<Func<Option<byte[]>>> GetImageBytes;

    public LocalTrackMetadata(
        string filePath,
        Option<string> title,
        Option<string[]> composers,
        Option<string[]> albumArtists,
        Option<string> album,
        Option<string[]> genres,
        Option<string> comment,
        Option<int> trackNumber,
        Option<int> year,
        Option<int> discNumber,
        Option<Func<Option<string>>> getImage,
        Option<Func<Option<byte[]>>> getImageBytes)
    {
        FilePath = filePath;
        Title = title;
        Composers = composers;
        AlbumArtists = albumArtists;
        Album = album;
        Genres = genres;
        Comment = comment;
        TrackNumber = trackNumber;
        Year = year;
        DiscNumber = discNumber;
        GetImageBytes = getImageBytes;
        GetImageFile = getImage;
    }
}