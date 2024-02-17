namespace Wavee.Spotify.Models.Interfaces;

public interface ISpotifyPlayableItem : ISpotifyItem
{
    TimeSpan Duration { get; } 
}