using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Items;
using Eum.UI.Services.Tracks;
using Eum.UI.ViewModels.Search.SearchItems;

namespace Eum.UI.ViewModels.Playlists
{
    public class PlaylistTrackViewModel : IIsPlaying
    {
        public PlaylistTrackViewModel(EumTrack eumTrack, int index)
        {
            Index = index;
            Track = eumTrack;
        }

        public PlaylistTrackViewModel(SpotifyTrackSearchItem searchTrackItem, int index)
        {
            Index = index;

            Track = new EumTrack(searchTrackItem);
        }

        public void RegisterEvents()
        {
            Ioc.Default.GetRequiredService<MainViewModel>()
                .PlaybackViewModel.PlayingItemChanged += PlaybackViewModelOnPlayingItemChanged;
        }

        public void UnregisterEvents()
        {
            Ioc.Default.GetRequiredService<MainViewModel>()
                .PlaybackViewModel.PlayingItemChanged -= PlaybackViewModelOnPlayingItemChanged;
        }

        private void PlaybackViewModelOnPlayingItemChanged(object sender, ItemId e)
        {
            if (_wasPlaying)
            {
                if (e != Track.Id)
                {
                    IsPlayingChanged?.Invoke(this, false);
                    _wasPlaying = false;
                }
            }
            else
            {
                if (e == Track.Id)
                {
                    IsPlayingChanged?.Invoke(this, true);
                    _wasPlaying = true;
                }
            }
        }

        private bool _wasPlaying;
        public int Index { get; }
        public EumTrack Track { get; }
        public bool IsPlaying()
        {
            return Ioc.Default.GetRequiredService<MainViewModel>()
                .PlaybackViewModel?.Item?.Id == Track.Id; 
        }

        public event EventHandler<bool>? IsPlayingChanged;
    }

    public interface IIsPlaying
    {
        bool IsPlaying();
        event EventHandler<bool> IsPlayingChanged;
        void RegisterEvents();
        void UnregisterEvents();
    }
}
