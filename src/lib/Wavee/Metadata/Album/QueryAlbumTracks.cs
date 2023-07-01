using Wavee.Id;
using Wavee.Metadata.Artist;

namespace Wavee.Metadata.Album;

internal readonly struct QueryAlbumTracks : IGraphQLQuery
{
    public QueryAlbumTracks(SpotifyId id)
    {
        Variables = new
        {
            uri = id.ToString(),
            offset = 0,
            limit = 300,
        };
    }

    private const string _hash = "8f7ebdeb93b6df4c31e6005d9ac29cde13d7543ce14d173e5e5e9599aafbcb9a";
    private const string _operationName = "queryAlbumTracks";
    public string OperationName => _operationName;
    public string Operationhash => _hash;
    public object Variables { get; }
}