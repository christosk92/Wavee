using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Features.Playlists.Queries;

namespace Wavee.UI.Features.Playlists.QueryHandlers;

public sealed class GetPlaylistSavedCountQueryHandler : IQueryHandler<GetPlaylistSavedCountQuery, ulong>
{
    private readonly ISpotifyClient _spotifyClient;

    public GetPlaylistSavedCountQueryHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public ValueTask<ulong> Handle(GetPlaylistSavedCountQuery query, CancellationToken cancellationToken)
    {
        return new ValueTask<ulong>(_spotifyClient.Playlists.GetPopCount(SpotifyId.FromUri(query.PlaylistId), cancellationToken));
    }
}