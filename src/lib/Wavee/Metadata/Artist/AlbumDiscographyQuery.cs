using Wavee.Id;

namespace Wavee.Metadata.Artist;

public readonly struct AlbumDiscographyQuery : IGraphQLQuery
{
    public AlbumDiscographyQuery(string queryname, SpotifyId artistId, int offset, int limit, string hash)
    {
        OperationName = queryname;
        Operationhash = hash;
        Variables = new
        {
            uri = artistId.ToString(),
            offset,
            limit
        };
    }

    public string OperationName { get; }
    public string Operationhash { get; }
    public object Variables { get; }
}