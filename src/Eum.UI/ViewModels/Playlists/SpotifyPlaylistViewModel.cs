using CommunityToolkit.Mvvm.DependencyInjection;
using DynamicData;
using Eum.Connections.Spotify;
using Eum.Spotify.playlist4;
using Eum.UI.Items;
using Eum.UI.Playlists;
using Eum.UI.Services.Tracks;
using Nito.AsyncEx;
using ReactiveUI;
using System.Reactive.Concurrency;
using Eum.Logging;
using AsyncLock = Nito.AsyncEx.AsyncLock;
using Exception = System.Exception;

namespace Eum.UI.ViewModels.Playlists;

public class SpotifyPlaylistViewModel : PlaylistViewModel
{
    public SpotifyPlaylistViewModel(EumPlaylist playlist) : base(playlist)
    {

    }

    private AsyncLock _syncLock = new AsyncLock();
    public override async Task Sync(bool addTracks = false)
    {
        //fetch the data..

        try
        {
            // https://spclient.wg.spotify.com/playlist/v2/playlist/37i9dQZF1E36Y1WP5YoeGC
            //TODO: ApResolver
            using (await _syncLock.LockAsync())
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
                    .GetTracks(uris)).ToArray();
                if (addTracks)
                {
                    for (var index = 0; index < tracks.Length; index++)
                    {
                        var eumTrack = tracks[index];
                        if (_tracksSourceList.Items.Any(j => j.Index == index))
                        {
                            //update
                        }
                        else
                        {
                            _tracksSourceList.Add(new PlaylistTrackViewModel(eumTrack, index));
                        }
                    }
                }

                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    Playlist.Tracks = uris;
                });

            }
        }
        catch (Exception x)
        {
            S_Log.Instance.LogError(x);
            throw;
        }
    }
}