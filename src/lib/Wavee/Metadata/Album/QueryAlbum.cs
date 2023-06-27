using System;
using System.Collections.Generic;
using System.Text;
using Wavee.Id;
using Wavee.Metadata.Artist;

namespace Wavee.Metadata.Album;

internal readonly struct QueryAlbum : IGraphQLQuery
{
    private const string _operationHash = "46ae954ef2d2fe7732b4b2b4022157b2e18b7ea84f70591ceb164e4de1b5d5d3";
    private const string _operationName = "getAlbum";

    public QueryAlbum(SpotifyId id, int offset, int limit)
    {
        Variables = new
        {
            uri = id.ToString(),
            offset = offset,
            limit = limit,
            locale = string.Empty
        };
    }

    public string OperationName => _operationName;

    public string Operationhash => _operationHash;

    // public string Query => _q;
    public object Variables { get; }
}