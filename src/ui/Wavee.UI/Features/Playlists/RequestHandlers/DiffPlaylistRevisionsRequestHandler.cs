using Eum.Spotify.playlist4;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Features.Playlists.Requests;

namespace Wavee.UI.Features.Playlists.RequestHandlers;

public sealed class DiffPlaylistRevisionsRequestHandler : IRequestHandler<DiffPlaylistRevisionsRequest, PlaylistDiffResult>
{
    private readonly ISpotifyClient _spotifyClient;

    public DiffPlaylistRevisionsRequestHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async ValueTask<PlaylistDiffResult> Handle(DiffPlaylistRevisionsRequest request, CancellationToken cancellationToken)
    {
        if (SpotifyId.TryParse(request.Id, out var id))
        {
            var spotifyDiff =
                await _spotifyClient.Playlists.DiffPlaylist(id, request.Revision, cancellationToken);

            return ToSharedDiffResult(spotifyDiff);
        }

        return new PlaylistDiffResult();
    }

    private PlaylistDiffResult ToSharedDiffResult(Diff spotifyDiff)
    {
        return new PlaylistDiffResult();
    }
}