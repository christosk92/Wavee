using Google.Protobuf;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;

namespace Wavee.UI.ViewModels.Playlists.Specific;

public sealed class PlaylistTrackViewModel
{
    public required int OriginalIndex { get; init; }
    public required ByteString Uid { get; init; }
    public required AudioId Id { get; init; }
    public required TrackOrEpisode View { get; init; }

    public string SmallestImage
    {
        get
        {
            var img = View.GetImageAsIndex(1);
            if (!string.IsNullOrEmpty(img)) return img;
            return "ms-appx://Assets/StoreLogo.png";
        }
    }
}