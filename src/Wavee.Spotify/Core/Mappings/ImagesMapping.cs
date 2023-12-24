using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Wavee.Core.Models.Common;
using Wavee.Spotify.Core.Extension;

namespace Wavee.Spotify.Core.Mappings;

public static class ImagesMapping
{
    private const string Url = "https://i.scdn.co/image/";
    public static ImmutableArray<UrlImage> MapToDto(this global::Spotify.Metadata.ImageGroup images)
    {
        var result = new UrlImage[images.Image.Count];
        for (var i = 0; i < images.Image.Count; i++)
        {
            var f = images.Image[i];
            result[i] = new UrlImage
            {
                Url = Url + f.FileId.Span.ToBase16(),
                Width = f.HasWidth ? (uint?)f.Width : null,
                Height = f.HasHeight ? (uint?)f.Height : null
            };
        }
        
        return ImmutableCollectionsMarshal.AsImmutableArray(result);
    }
}