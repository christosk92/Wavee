using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Models.Track;

public readonly record struct SpotifyPlayableItemDescription : ISpotifyItem
{
    public required string Name { get; init; }
    public required SpotifyId Uri { get; init; }
}