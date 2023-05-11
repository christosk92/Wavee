namespace Wavee.Spotify.Clients.Token;

public interface ITokenClient
{
    ValueTask<string> GetToken(CancellationToken token = default);
}