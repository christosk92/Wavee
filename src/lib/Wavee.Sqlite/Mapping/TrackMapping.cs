using Google.Protobuf;
using Spotify.Metadata;
using System.Text;
using Wavee.Sqlite.Entities;

namespace Wavee.Sqlite.Mapping;

internal static class TrackMapping
{
    public static Track ToTrack(this CachedTrack cached)
    {
        return Track.Parser.ParseFrom(cached.OriginalData);
    }

    public static CachedTrack ToCachedTrack(this Track track, string id, DateTimeOffset expiration)
    {
        return new CachedTrack
        {
            Id = id,
            Name = track.Name,
            MainArtistName = track.Artist.First().Name,
            AlbumName = track.Album.Name,
            AlbumDiscNumber = track.DiscNumber,
            AlbumTrackNumber = track.Number,
            Duration = track.Duration,
            TagsCommaSeparated = string.Join(",", track.Tags),
            SmallImageId = GetImage(track.Album.CoverGroup, Image.Types.Size.Small),
            MediumImageId = GetImage(track.Album.CoverGroup, Image.Types.Size.Default),
            LargeImageId = GetImage(track.Album.CoverGroup, Image.Types.Size.Large),
            OriginalData = track.ToByteArray(),
            CacheExpiration = expiration
        };
    }

    private static string GetImage(ImageGroup albumCoverGroup, Image.Types.Size size)
    {
        var requestedSize = albumCoverGroup.Image.FirstOrDefault(x => x.Size == size);
        if (requestedSize != null)
        {
            return ToHex(requestedSize.FileId.Span);
        }
        var firstOne = albumCoverGroup.Image.FirstOrDefault();
        if (firstOne != null)
        {
            return ToHex(firstOne.FileId.Span);
        }

        return string.Empty;
    }

    private static string ToHex(ReadOnlySpan<byte> fileIdSpan)
    {
        var hex = new StringBuilder(fileIdSpan.Length * 2);
        foreach (var b in fileIdSpan)
        {
            hex.Append($"{b:x2}");
        }

        return hex.ToString();
    }
}