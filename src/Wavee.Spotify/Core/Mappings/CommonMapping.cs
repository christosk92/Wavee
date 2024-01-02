using Spotify.Metadata;
using Wavee.Core.Models.Common;
using Wavee.Spotify.Core.Extension;
using Wavee.Spotify.Core.Models.Track;

namespace Wavee.Spotify.Core.Mappings;

internal static class CommonMapping
{
    public static UrlImage MapToDto(this Image urlImage)
    {
        const string url = "https://i.scdn.co/image/";
        return new UrlImage
        {
            Url = url + urlImage.FileId.Span.ToBase16(),
            Width = urlImage.HasWidth ? (ushort)urlImage.Width : null,
            Height = urlImage.HasHeight ? (ushort)urlImage.Height : null,
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