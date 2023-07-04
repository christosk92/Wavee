using Eum.Spotify.playlist4;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Id;
using Wavee.Sqlite.Entities;
using Wavee.UI.Client.Playlist;
using Wavee.UI.Client.Playlist.Models;
using Wavee.UI.ViewModel.Playlist;
using Wavee.UI.ViewModel.Playlist.Headers;

namespace Wavee.UI.Spotify.Clients.Playlist;

internal sealed class SpotifyUIPlaylistClient : IWaveeUIPlaylistClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;

    public SpotifyUIPlaylistClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public async ValueTask<WaveeUIPlaylist> GetPlaylist(string id, CancellationToken cancellationToken)
    {
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        var cache = spotifyClient.Cache;
        var spotifyId = SpotifyId.FromUri(id);
        var potentialCacheHit = await cache.TryGetPlaylist(spotifyId, cancellationToken);

        static WaveeUIPlaylist ToUIPlaylist(SelectedListContent listContent,
            bool fromCache, string id)
        {
            var tracks = new WaveeUIPlaylistTrackInfo[listContent.Contents.Items.Count];

            for (int i = 0; i < tracks.Length; i++)
            {
                var item = listContent.Contents.Items[i];
                //var metaItem = listContent.Contents.MetaItems[i];

                tracks[i] = new WaveeUIPlaylistTrackInfo(
                    Id: SpotifyId.FromUri(item.Uri).ToString(),
                    Uid: item.Attributes.ItemId.ToBase64(),
                    AddedAt: item.Attributes.HasTimestamp && item.Attributes.Timestamp is not 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(item.Attributes.Timestamp)
                        : Option<DateTimeOffset>.None,
                    AddedBy: item.Attributes.HasAddedBy && item.Attributes.AddedBy is not null
                        ? item.Attributes.AddedBy
                        : Option<string>.None,
                    Metadata: new HashMap<string, string>()
                );
            }

            var isBigHeader = listContent.Attributes.FormatAttributes.Any(x => x.Key is "header_image_url_desktop");
            var futureMozaicTracks = new TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>>(TaskCreationOptions.RunContinuationsAsynchronously);

            return new WaveeUIPlaylist
            {
                Revision = PlaylistRevisionId.FromByteString(listContent.Revision),
                Id = id,
                FromCache = fromCache,
                Tracks = tracks,
                FutureTracks = futureMozaicTracks,
                Header = isBigHeader ? new PlaylistBigHeader(listContent) : new RegularPlaylistHeader(listContent, futureMozaicTracks),
                Name = listContent.Attributes.Name,
                ImageId = Option<string>.None,
                Description = listContent.Attributes.HasDescription ? listContent.Attributes.Description
                    : string.Empty,
                Owner = listContent.OwnerUsername
            };
        }


        return await potentialCacheHit
            .MatchAsync(Some: playlist =>
            {
                var selectedListContent = SelectedListContent.Parser.ParseFrom(playlist.Data);
                return ToUIPlaylist(selectedListContent, true, id);
            }, None: async () =>
            {
                try
                {
                    var playlistResult = await spotifyClient.Metadata.GetPlaylist(spotifyId, cancellationToken);
                    //insert into cache
                    await cache.SavePlaylist(spotifyId, playlistResult, cancellationToken);
                    return ToUIPlaylist(playlistResult, false, id);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Failed to get playlist {id}", e);
                }
            });
    }
}