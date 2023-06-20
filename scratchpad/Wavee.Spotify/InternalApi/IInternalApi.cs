namespace Wavee.Spotify.InternalApi;

public interface IInternalApi
{
    /// <summary>
    /// Does a GET request to the Spotify partner api (api-partner.spotify.com). 
    /// </summary>
    /// <param name="operationName">
    ///  The name of the operation to execute.
    /// </param>
    /// <param name="operationHash">
    /// The persisted hash of the operation to execute.
    /// </param>
    /// <param name="variables">
    ///  Any variables to pass to the operation.
    /// </param>
    /// <param name="ct">
    ///  A cancellation token.
    /// </param>
    /// <returns>
    ///  The response from the api.
    /// </returns>
    Task<HttpResponseMessage> GetPartner(string operationName, string operationHash, object? variables = null, CancellationToken ct = default);
}