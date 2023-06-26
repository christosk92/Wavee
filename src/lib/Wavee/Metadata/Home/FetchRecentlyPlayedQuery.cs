using Wavee.Metadata.Artist;

namespace Wavee.Metadata.Home;

public readonly record struct FetchRecentlyPlayedQuery(string[] Uris) : IGraphQLQuery
{
    private const string _operationName = "fetchEntitiesForRecentlyPlayed";
    private const string _operationHash = "3f18c5ce10e4a1bc8e710b86c4b549c540056cfd5fbdf85702947d4263578cff";
    public string OperationName => _operationName;
    public string Operationhash => _operationHash;

    public object Variables => new
    {
        uris = Uris
    };
}