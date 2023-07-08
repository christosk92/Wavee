using System.Net;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Eum.Spotify.playlist4;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Org.BouncyCastle.Utilities.Encoders;
using Wavee.Id;
using Wavee.Sqlite.Entities;
using Wavee.UI.Client.Playlist;
using Wavee.UI.Client.Playlist.Models;
using Wavee.UI.ViewModel.Playlist;
using Wavee.UI.ViewModel.Playlist.Headers;
using Wavee.UI.ViewModel.Playlist.User;

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
            var futureMozaicTracks =
                new TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>>(TaskCreationOptions
                    .RunContinuationsAsynchronously);

            return new WaveeUIPlaylist
            {
                Revision = PlaylistRevisionId.FromByteString(listContent.Revision),
                Id = id,
                FromCache = fromCache,
                Tracks = tracks,
                FutureTracks = futureMozaicTracks,
                Header = isBigHeader
                    ? new PlaylistBigHeader(listContent)
                    : new RegularPlaylistHeader(listContent,
                        tracksDurationStirng: new ObservableStringHolder(),
                        tracksCountString: new ObservableStringHolder()
                    )
                    {
                        FutureTracks = new FutereTracksHolder(futureMozaicTracks)
                    },
                Name = listContent.Attributes.Name,
                ImageId = Option<string>.None,
                Description = listContent.Attributes.HasDescription
                    ? listContent.Attributes.Description
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

    public IObservable<PlaylistInfoNotification> ListenForUserPlaylists()
    {
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        return spotifyClient.Remote
            .CreatePlaylistListener()
            .StartWith(Unit.Default)
            .SelectMany(async _ => await spotifyClient.Metadata.GetUserRootList())
            .Select(x =>
            {
                var output = new List<PlaylistInfo>();
                Stack<PlaylistInfo> foldersStack = new Stack<PlaylistInfo>();
                for (var index = 0; index < x.Contents.Items.Count; index++)
                {
                    var info = x.Contents.Items[index];
                    var metaItem = x.Contents.MetaItems[index];
                    //spotify:start-group:0c84ac461d050af8:New+Folder
                    //spotify:spotify:end-group:0c84ac461d050af8
                    var uri = info.Uri;
                    if (uri.StartsWith("spotify:start-group:"))
                    {
                        var folderNameUrlsafe = uri.Split(':').LastOrDefault() ?? "New folder";
                        //contains + etc for space
                        var folderName = WebUtility.UrlDecode(folderNameUrlsafe);
                        foldersStack.Push(new PlaylistInfo
                        {
                            Id = uri,
                            Name = folderName,
                            OwnerId = spotifyClient.WelcomeMessage.CanonicalUsername,
                            IsFolder = true,
                            Children = new List<PlaylistInfo>()
                        });
                    }
                    else if (uri.StartsWith("spotify:end-group"))
                    {
                        //end of folder
                        var folder = foldersStack.Pop();
                        if (foldersStack.Count > 0)
                        {
                            foldersStack.Peek().Children.Add(folder);
                        }
                        else
                        {
                            output.Add(folder);
                        }
                    }
                    else
                    {

                        var playlistInfo = new PlaylistInfo
                        {
                            Id = uri,
                            Name = metaItem.Attributes.Name,
                            OwnerId = metaItem.HasOwnerUsername && metaItem.OwnerUsername is not null
                                ? metaItem.OwnerUsername
                                : spotifyClient.WelcomeMessage.CanonicalUsername,
                            IsFolder = false,
                            Children = new List<PlaylistInfo>()
                        };
                        if (foldersStack.Count > 0)
                        {
                            foldersStack.Peek().Children.Add(playlistInfo);
                        }
                        else
                        {
                            output.Add(playlistInfo);
                        }
                    }
                }

                return new PlaylistInfoNotification
                {
                    ChangeType = PlaylistInfoChangeType.Add,
                    Playlists = Unsafe.As<List<PlaylistInfo>, PlaylistInfo[]>(ref output)
                };
            });
    }
}