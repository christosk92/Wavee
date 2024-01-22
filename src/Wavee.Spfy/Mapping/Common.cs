using Spotify.Metadata;
using Wavee.Spfy.Items;
using Wavee.Spfy.Utils;

namespace Wavee.Spfy.Mapping;

internal static class CommonMapping
{
    public static UrlImage MapToDto(this Image urlImage)
    {
        const string url = "https://i.scdn.co/image/";
        return new UrlImage
        {
            Url = url + urlImage.FileId.Span.ToBase16(),
            Width = urlImage.HasWidth
                ? (ushort)urlImage.Width
                : null,
            Height = urlImage.HasHeight
                ? (ushort)urlImage.Height
                : null,
            CommonSize = urlImage.Size switch
            {
                Image.Types.Size.Default => UrlImageSizeType.Medium,
                Image.Types.Size.Small => UrlImageSizeType.Small,
                Image.Types.Size.Large => UrlImageSizeType.Large,
                Image.Types.Size.Xlarge => UrlImageSizeType.ExtraLarge,
                _ => throw new ArgumentOutOfRangeException()
            }
        };
    }

    public static SpotifyAudioFile MapToDto(this AudioFile audioFile)
    {
        return new SpotifyAudioFile
        {
            FileId = audioFile.FileId.ToByteArray(),
            Format = audioFile.Format
        };
    }
}