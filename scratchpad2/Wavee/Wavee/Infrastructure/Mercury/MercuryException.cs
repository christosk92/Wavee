using Wavee.Token.Live;

namespace Wavee.Infrastructure.Mercury;

public sealed class MercuryException : Exception
{
    internal MercuryException(MercuryResponse response) : base(
        $"Mercury request for uri: {response.Header.Uri} failed with status code: {response.Header.StatusCode}")
    {
        Response = response;
    }

    public MercuryResponse Response
    {
        get;
    }
}