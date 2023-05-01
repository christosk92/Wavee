using Wavee.Spotify.Infrastructure.Live;
using Wavee.Spotify.Infrastructure.Traits;
using Wavee.Spotify.Models;

namespace Wavee.Spotify;

public record SpotifyClientBuilder(SpotifySessionConfig Config)
{
    public static SpotifyClientBuilder New() => new(SpotifySessionConfig.Default);

    public SpotifyClientBuilder WithConfig(SpotifySessionConfig config) =>
        this with { Config = config };

    public ISpotifyClient Build() => new LiveSpotifyClient(Config);
}