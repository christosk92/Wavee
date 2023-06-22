namespace Wavee.Token;

public interface ITokenClient
{
    ValueTask<string> GetToken(CancellationToken ct = default);
}