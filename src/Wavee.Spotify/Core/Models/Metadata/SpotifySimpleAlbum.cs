using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Models.Metadata;

public readonly struct SpotifySimpleAlbum : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
}