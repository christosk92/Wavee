namespace Wavee.Spotify.Infrastructure.Mercury;

public interface ISpotifyMercuryClient
{
    Task<string> GetAccessToken(CancellationToken ct = default);
}