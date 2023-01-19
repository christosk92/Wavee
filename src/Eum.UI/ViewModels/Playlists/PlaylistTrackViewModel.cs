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
    public partial  class PlaylistTrackViewModel : IIsPlaying, IIsSaved, IEquatable<PlaylistTrackViewModel>, IEqualityComparer<PlaylistTrackViewModel>
    {
        private int _originalIndex;
        [ObservableProperty] private int _index;
        [ObservableProperty] private bool _isSaved;
        public PlaylistTrackViewModel(EumTrack eumTrack, int index, ICommand playCommand)
        {
            _originalIndex = index;
            Index = index;
            Track = eumTrack;
            PlayCommand = playCommand;
        }

        public PlaylistTrackViewModel(SpotifyTrackSearchItem searchTrackItem, int index)
        {
            _originalIndex = index;
            Index = index;
            Track = new EumTrack(searchTrackItem);

        }

        public ICommand PlayCommand { get; }

        public EumTrack Track { get; }
        public ItemId Id => Track.Id;
        public int OriginalIndex => _originalIndex;
        public bool Equals(PlaylistTrackViewModel? other)
        {
            return Id == other?.Id && 
                   _originalIndex == 
                other?._originalIndex && Id == other.Id;
        }


        public bool Equals(PlaylistTrackViewModel x, PlaylistTrackViewModel y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return 
                x.Id == y.Id &&
                x._originalIndex == y._originalIndex;
        }

        public int GetHashCode(PlaylistTrackViewModel obj)
        {
            return HashCode.Combine(obj._originalIndex, Id.GetHashCode());
        }
    }

    public interface IIsPlaying
    {
        ItemId Id { get; }
        // void RegisterEvents();
        // void UnregisterEvents();
    }
}
