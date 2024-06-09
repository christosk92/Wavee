using Wavee.Contracts.Interfaces;

namespace Wavee.UI.Spotify.Common;

public interface ISpotifyId : IItemId
{
    string AsString { get; }
    SpotifyIdItemType Type { get; }
}