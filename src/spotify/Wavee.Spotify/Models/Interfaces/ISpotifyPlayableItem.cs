using Wavee.Core;

namespace Wavee.Spotify.Models.Interfaces;

public interface ISpotifyPlayableItem : ISpotifyItem, IWaveePlayableItem
{
    TimeSpan Duration { get; } 
}