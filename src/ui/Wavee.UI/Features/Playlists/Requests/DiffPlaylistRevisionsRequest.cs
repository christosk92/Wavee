using System.Numerics;
using Mediator;

namespace Wavee.UI.Features.Playlists.Requests;

public sealed class DiffPlaylistRevisionsRequest : IRequest<PlaylistDiffResult>
{
    public required string Id { get; init; }
    public required BigInteger Revision { get; init; }
}

public sealed class PlaylistDiffResult
{
}