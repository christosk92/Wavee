namespace Wavee.Spotify.Models;

public readonly record struct SpotifySessionConfig(string DeviceId)
{
    public static SpotifySessionConfig Default = new SpotifySessionConfig(
        DeviceId: Guid.NewGuid().ToString()
    );
}