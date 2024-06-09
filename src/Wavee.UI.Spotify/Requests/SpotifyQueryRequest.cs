using Refit;

namespace Wavee.UI.Spotify.Requests;

internal abstract class SpotifyQueryRequest<TResponse>
{
    protected SpotifyQueryRequest(string operationName, string operationHash)
    {
        OperationName = operationName;
        Extensions = "{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"" + operationHash + "\"}}";
    }
    
    [AliasAs("operationName")] 
    public string OperationName { get; }
    [AliasAs("extensions")]
    public string Extensions { get; }
}