namespace Wavee.Spotify.Mercury;

public interface IMercuryClient
{
    ValueTask<string> GetToken(CancellationToken ct = default);
    Task<MercuryResponse> GetAsync(string endpoint, CancellationToken cancellationToken = default);
}