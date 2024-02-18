using System.Collections.Immutable;
using System.Numerics;
using Google.Protobuf.Collections;
using Spotify.Metadata;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Models.Mapping;

internal static class TrackMapping
{
    public static SpotifyTrackInfo MapToDto(this Track track, SpotifyId id)
    {
        return new SpotifyTrackInfo
        {
            Uri = id,
            Name = track.Name,
            Number = track.Number,
            DiscNumber = track.DiscNumber,
            HasLyrics = track.HasLyrics,
            Album = track.Album.MapToTrackAlbum(),
            Artists = track.Artist.Select(x => x.MapToTrackArtist())
                .ToImmutableList(),
            Duration = TimeSpan.FromMilliseconds(track.Duration),
            AudioFiles = track.File.Where(x =>
                    x.Format is AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.OggVorbis160
                        or AudioFile.Types.Format.OggVorbis320)
                .Select(x => new SpotifyAudioFile(Type: x.Format switch
                {
                    AudioFile.Types.Format.OggVorbis96 => SpotifyAudioFileType.OGG_VORBIS_96,
                    AudioFile.Types.Format.OggVorbis160 => SpotifyAudioFileType.OGG_VORBIS_160,
                    AudioFile.Types.Format.OggVorbis320 => SpotifyAudioFileType.OGG_VORBIS_320,
                    _ => throw new ArgumentOutOfRangeException()
                }, x.FileId))
                .ToImmutableList()
        };
    }

    public static SpotifyTrackArtist MapToTrackArtist(this Artist artist)
    {
        return new SpotifyTrackArtist
        {
            Uri = SpotifyId.FromRaw(artist.Gid.Span, AudioItemType.Artist),
            Name = artist.Name
        };
    }

    public static SpotifyTrackAlbum MapToTrackAlbum(this Album album)
    {
        DeserializeImages(album.CoverGroup.Image, out var large, out var medium, out var small);

        return new SpotifyTrackAlbum
        {
            Uri = SpotifyId.FromRaw(album.Gid.Span, AudioItemType.Album),
            Name = album.Name,
            ReleaseDate = album.Date.HasDay
                ? new DateOnly(album.Date.Year, album.Date.Month, album.Date.Day)
                : album.Date.HasMonth
                    ? new DateOnly(album.Date.Year, album.Date.Month, 1)
                    : new DateOnly(album.Date.Year, 1, 1),
            Artists = album.Artist.Select(x => x.MapToTrackArtist()).ToImmutableArray(),
            Label = album.Label,
            LargeImageUrl = large,
            MediumImageUrl = medium,
            SmallImageUrl = small
        };
    }

    private static void DeserializeImages(this RepeatedField<Image> images, out string? largeImage,
        out string? mediumImage, out string? smallImage)
    {
        const string cdn = "https://i.scdn.co/image/";

        largeImage = mediumImage = smallImage = null;
        ushort? largeSize = null, mediumSize = null, smallSize = null;
        (string, ushort?, ushort?)? previous = null;
        foreach (var image in images)
        {
            ushort? heightMaybe = image.HasHeight ? (ushort)image.Height : (ushort?)null;
            var url = cdn + image.FileId.Span.ToBase16();

            if (!largeSize.HasValue || heightMaybe > largeSize)
            {
                // Shift down the sizes
                smallSize = mediumSize;
                smallImage = mediumImage;
                mediumSize = largeSize;
                mediumImage = largeImage;
                // Set new largest
                largeSize = heightMaybe;
                largeImage = url;
            }
            else if (!mediumSize.HasValue || heightMaybe > mediumSize)
            {
                // Shift down the sizes
                smallSize = mediumSize;
                smallImage = mediumImage;
                // Set new medium
                mediumSize = heightMaybe;
                mediumImage = url;
            }
            else if (!smallSize.HasValue || heightMaybe < smallSize)
            {
                // Set new smallest
                smallSize = heightMaybe;
                smallImage = url;
            }
        }
    }
}