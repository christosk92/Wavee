using Wavee.Interfaces.Models;
using Wavee.Spotify.Core.Models.Common;

namespace Wavee.Spotify.Interfaces.Models;

public interface ISpotifyPlayableItem : ISpotifyItem, IWaveePlayableItem
{
    SpotifyId Uri { get; }
}