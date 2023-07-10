using System.Net;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using Eum.Spotify.playlist4;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Org.BouncyCastle.Utilities.Encoders;
using Serilog;
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
            bool fromCache, string id, SpotifyUIPlaylistClient spotifyUiPlaylistClient)
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
                    : new RegularPlaylistHeader(
                        id: id,
                        listContent,
                        tracksDurationStirng: new ObservableStringHolder(),
                        tracksCountString: new ObservableStringHolder(),
                        saveCommand: new AsyncRelayCommand<string>(spotifyUiPlaylistClient.SavePlaylist)
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
                return ToUIPlaylist(selectedListContent, true, id, this);
            }, None: async () =>
            {
                try
                {
                    var playlistResult = await spotifyClient.Metadata.GetPlaylist(spotifyId, cancellationToken);
                    //insert into cache
                    await cache.SavePlaylist(spotifyId, playlistResult, cancellationToken);
                    return ToUIPlaylist(playlistResult, false, id, this);
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
                int actualIndex = 0;
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
                            Children = new List<PlaylistInfo>(),
                            FixedIndex = actualIndex++
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
                            Children = new List<PlaylistInfo>(),
                            FixedIndex = actualIndex
                        };
                        if (foldersStack.Count > 0)
                        {
                            playlistInfo.FixedIndex = foldersStack.Peek().Children.Count;
                            foldersStack.Peek().Children.Add(playlistInfo);
                        }
                        else
                        {
                            output.Add(playlistInfo);
                            actualIndex++;
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

    public async Task SavePlaylist(string id, CancellationToken ct = default)
    {
        try
        {
            var changes = new ListChanges
            {
                WantResultingRevisions = false,
                WantSyncResult = false,
                Nonces = { },
                Deltas =
                {
                    new Delta
                    {
                        Info = new ChangeInfo
                        {
                            Source = new SourceInfo
                            {
                                Client = SourceInfo.Types.Client.Webplayer
                            },
                        },
                        Ops =
                        {
                            new Op
                            {
                                Kind = Op.Types.Kind.Add,
                                Add = new Add
                                {
                                    Items =
                                    {
                                        new Item
                                        {
                                            Uri = id,
                                            Attributes = new ItemAttributes
                                            {
                                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                                FormatAttributes = { }
                                            }
                                        }
                                    },
                                    AddFirst = true
                                }
                            }
                        }
                    }
                }
            };

            if (!_spotifyClient.TryGetTarget(out var cl))
                throw new ObjectDisposedException(nameof(SpotifyClient));

            var playlistId = $"user/{cl.WelcomeMessage.CanonicalUsername}/rootlist";
            await cl.Metadata.WritePlaylistChanges(playlistId, changes, ct);
            return;
        }
        catch (Exception x)
        {
            Log.Error(x, "An error occurred while trying to follow: {id}", id);
            return;
        }
    }
}