using Spotify.Metadata;
using Wavee.Models.Common;
using Wavee.Models.Metadata;

namespace Wavee.Helpers;

internal static class ImageProcessor
{
    // Method to process album cover images and assign sizes
    public static Dictionary<SpotifyImageSize, SpotifyImage> ProcessAlbumCovers(Track trackMessage)
    {
        var images = new Dictionary<SpotifyImageSize, SpotifyImage>();
        var albumCovers = trackMessage.Album?.CoverGroup?.Image;

        // Check if there are any album covers
        if (albumCovers == null || !albumCovers.Any())
        {
            return images; // Return empty dictionary if no images
        }

        // Sort the album covers by width in ascending order
        var sortedCovers = albumCovers.OrderBy(ac => ac.Width).ToList();
        int count = sortedCovers.Count;

        const string CDN_URL = "https://i.scdn.co/image/";

        if (count == 1)
        {
            // Only one image: Size is null
            var singleCover = sortedCovers[0];
            var fileId = new FileId(singleCover.FileId.ToByteArray());
            var url = $"{CDN_URL}{fileId.ToBase16()}";

            var image = new SpotifyImage
            {
                Size = SpotifyImageSize.Default,
                Url = new Uri(url),
                Width = singleCover.Width
            };

            images[SpotifyImageSize.Default] = image;
        }
        else
        {
            // Assign Small to the smallest width
            var smallCover = sortedCovers.First();
            var smallFileId = new FileId(smallCover.FileId.ToByteArray());
            var smallUrl = $"{CDN_URL}{smallFileId.ToBase16()}";

            var smallImage = new SpotifyImage
            {
                Size = SpotifyImageSize.Small,
                Url = new Uri(smallUrl),
                Width = smallCover.Width
            };

            images[SpotifyImageSize.Small] = smallImage;

            // Assign Large to the largest width
            var largeCover = sortedCovers.Last();
            var largeFileId = new FileId(largeCover.FileId.ToByteArray());
            var largeUrl = $"{CDN_URL}{largeFileId.ToBase16()}";

            var largeImage = new SpotifyImage
            {
                Size = SpotifyImageSize.Large,
                Url = new Uri(largeUrl),
                Width = largeCover.Width
            };

            images[SpotifyImageSize.Large] = largeImage;

            if (count >= 3)
            {
                // Assign Medium to the middle width
                var middleIndex = count / 2; // Integer division for middle index
                var mediumCover = sortedCovers[middleIndex];
                var mediumFileId = new FileId(mediumCover.FileId.ToByteArray());
                var mediumUrl = $"{CDN_URL}{mediumFileId.ToBase16()}";

                var mediumImage = new SpotifyImage
                {
                    Size = SpotifyImageSize.Medium,
                    Url = new Uri(mediumUrl),
                    Width = mediumCover.Width
                };

                images[SpotifyImageSize.Medium] = mediumImage;
            }
        }

        return images;
    }
}