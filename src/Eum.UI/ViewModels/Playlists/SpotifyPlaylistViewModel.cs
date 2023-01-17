using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Spotify.playlist4;
using Eum.UI.Items;
using Eum.UI.Playlists;
using Eum.UI.Services.Tracks;
using Eum.Logging;
using AsyncLock = Nito.AsyncEx.AsyncLock;
using Exception = System.Exception;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Eum.Enums;
using Eum.UI.Services;

namespace Eum.UI.ViewModels.Playlists;

public class SpotifyPlaylistViewModel : PlaylistViewModel
{
    public SpotifyPlaylistViewModel(EumPlaylist playlist) : base(playlist)
    {

    }

    private bool _isSorting = false;
    private AsyncLock _syncLock = new AsyncLock();

    public override async Task Sync(bool addTracks = false)
    {
        //fetch the data..

        try
        {
            // https://spclient.wg.spotify.com/playlist/v2/playlist/37i9dQZF1E36Y1WP5YoeGC
            using (await _syncLock.LockAsync())
            {
                if (addTracks)
                {
                    //first add the items we already know
                    var filtered = _playlist.Tracks?.Where(a => a.Type != EntityType.Unknown)?.ToArray();
                    if (filtered is
                        {
                            Length: > 0
                        })
                    {
                        var tracks = (await Ioc.Default.GetRequiredService<ITrackAggregator>()
                            .GetTracks(filtered));
                        var asVms = ToVm(tracks.Where(a=> a != null));
                        Ioc.Default.GetRequiredService<IDispatcherHelper>()
                            .TryEnqueue(QueuePriority.High, () =>
                            {
                                Tracks.AddRange(asVms);
                            });
                    }
                }

                await Task.Run(() => FetchNewItems(addTracks));
            }
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
            throw;
        }
    }

    private async Task FetchNewItems(bool addTracks)
    {
        var spotifyClient = Ioc.Default.GetRequiredService<ISpotifyClient>();

        using var playlistResponse =
            await spotifyClient.SpClientPlaylists.GetPlaylist(Playlist.Id.Id, CancellationToken.None);

        var data = SelectedListContent.Parser.ParseFrom(await (playlistResponse.Content.ReadAsStreamAsync()));

        //TODO: merge with local tracks
        var uris = data
            .Contents.Items.Select(a => new ItemId(a.Uri))
            .ToArray();
        var tracks = (await Ioc.Default.GetRequiredService<ITrackAggregator>()
            .GetTracks(uris));
        if (addTracks)
        {
            var assARr = ToVm(tracks);
            // Ioc.Default.GetRequiredService<IDispatcherHelper>()
            //     .TryEnqueue(QueuePriority.High, () => { Tracks.AddRange(assARr); });
            // for (var index = 0; index < tracks.Length; index++)
            // {
            //     var eumTrack = tracks[index];
            //     if (_tracksSourceList.Items.Any(j => j.Index == index))
            //     {
            //         //update
            //     }
            //     else
            //     {
            //         _tracksSourceList.Add(new PlaylistTrackViewModel(eumTrack, index));
            //     }
            // }
        }

        Ioc.Default.GetRequiredService<IDispatcherHelper>()
            .TryEnqueue(QueuePriority.High, () =>
            {
                Playlist.Tracks = uris;
            });
    }

    private IEnumerable<PlaylistTrackViewModel> ToVm(IEnumerable<EumTrack> tracks)
    {
        var main = Ioc.Default.GetRequiredService<MainViewModel>();
        var playCommand = new AsyncRelayCommand<int>(async index =>
        {
            try
            {
                if (_isSorting)
                {
                    var allPages = Tracks.Select(a => new PagedTrack(a.Id, new Dictionary<string, string>
                    {
                        {"original_index", _playlist.Tracks.IndexOf(a.Id).ToString()}
                    }));
                    await Ioc.Default.GetRequiredService<IPlaybackService>()
                        .PlayOnDevice(new PagedContextPlayCommand(Id,
                            allPages.ToArray(),
                            index, Playlist.Metadata.ToDictionary(a => a.Key, a => a.Value)));
                }
                else
                {
                    await Ioc.Default.GetRequiredService<IPlaybackService>()
                        .PlayOnDevice(new PlainContextPlayCommand(Id,
                            index, Playlist.Metadata.ToDictionary(a => a.Key, a => a.Value)));
                }
            }
            catch (Exception notImplementedException)
            {
                await Ioc.Default.GetRequiredService<IErrorMessageShower>()
                    .ShowErrorAsync(notImplementedException, "Unexpected error",
                        "Something went wrong while trying to play the track.");
            }
        });

      return tracks.Select((a, i) => new PlaylistTrackViewModel(a, i, playCommand)
        {
            IsSaved = main.CurrentUser.User.LibraryProvider.IsSaved(a.Id),
        });
    }
}