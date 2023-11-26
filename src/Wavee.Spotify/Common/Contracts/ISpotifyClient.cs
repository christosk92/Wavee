namespace Wavee.Spotify.Common.Contracts;

public interface ISpotifyClient
{
    Task<string> Test();
}