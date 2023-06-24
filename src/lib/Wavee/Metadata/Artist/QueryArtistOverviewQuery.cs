using System.Security.Cryptography;
using System.Text;
using Wavee.Id;

namespace Wavee.Metadata.Artist;

internal readonly struct QueryArtistOverviewQuery : IGraphQLQuery
{
    private const string _operationHash = "35648a112beb1794e39ab931365f6ae4a8d45e65396d641eeda94e4003d41497";
    private const string _operationName = "queryArtistOverview";

    public QueryArtistOverviewQuery(SpotifyId id, bool includePrerelease)
    {
        Variables = new
        {
            uri = id.ToString(),
            locale = string.Empty,
            includePrerelease = includePrerelease
        };
    }

    public string OperationName => _operationName;

    public string Operationhash => _operationHash;

    // public string Query => _q;
    public object Variables { get; }
}

internal interface IGraphQLQuery
{
    string OperationName { get; }
    string Operationhash { get; }
    object Variables { get; }
}