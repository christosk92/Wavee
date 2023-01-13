using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Items;
using Eum.UI.Services.Tracks;
using Eum.UI.ViewModels.Artists;
using Eum.UI.ViewModels.Search.SearchItems;
using Eum.Users;

namespace Eum.UI.ViewModels.Playlists
{
    [INotifyPropertyChanged]
    public partial  class PlaylistTrackViewModel : IIsPlaying, IIsSaved
    {
        [ObservableProperty] private int _index;
        [ObservableProperty] private bool _isSaved;
        public PlaylistTrackViewModel(EumTrack eumTrack, int index, ICommand playCommand)
        {
            Index = index;
            Track = eumTrack;
            PlayCommand = playCommand;
        }

        public PlaylistTrackViewModel(SpotifyTrackSearchItem searchTrackItem, int index)
        {
            Index = index;

            Track = new EumTrack(searchTrackItem);

        }

        public ICommand PlayCommand { get; }

        public EumTrack Track { get; }
        public ItemId Id => Track.Id;

    }

    public interface IIsPlaying
    {
        ItemId Id { get; }
        // void RegisterEvents();
        // void UnregisterEvents();
    }
}
