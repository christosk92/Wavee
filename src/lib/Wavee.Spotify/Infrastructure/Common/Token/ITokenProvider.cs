namespace Wavee.Spotify.Infrastructure.Common.Token;

public interface ITokenProvider
{
    ValueTask<string> GetToken();
}