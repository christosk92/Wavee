using System.Diagnostics.CodeAnalysis;
using System.Text;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Spotify.Metadata;
using Wavee.Spotify.Clients.Info;

namespace Wavee.Spotify.Id;

public readonly struct ImageId
{
    public static void PutAsMetadata([NotNull] ProvidedTrack builder,
        [NotNull] ImageGroup group)
    {
        foreach (var image in group.Image)
        {
            String key;
            switch (image.Size)
            {
                case Image.Types.Size.Default:
                    key = "image_url";
                    break;
                case Image.Types.Size.Small:
                    key = "image_small_url";
                    break;
                case Image.Types.Size.Large:
                    key = "image_large_url";
                    break;
                case Image.Types.Size.Xlarge:
                    key = "image_xlarge_url";
                    break;
                default:
                    continue;
            }

            builder.Metadata[key] =
                $"spotify:image:{SpotifyId.FromRaw(image.FileId.Span, AudioItemType.Image).ToBase16()}";
        }
    }

    public SpotifyId Id { get; init; }

    public ImageId(ByteString hexByteString)
    {
        Id = SpotifyId.FromRaw(hexByteString.Span, AudioItemType.Image);
    }
}