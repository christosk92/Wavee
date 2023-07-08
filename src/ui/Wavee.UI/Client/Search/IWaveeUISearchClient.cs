namespace Wavee.UI.Client.Search;

public interface IWaveeUISearchClient
{
    Task<ReadOnlyMemory<byte>> GetSearchResultsAsync(string query, CancellationToken cancellationToken = default);
}