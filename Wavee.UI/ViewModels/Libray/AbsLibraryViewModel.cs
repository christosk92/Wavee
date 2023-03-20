using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Wavee.Enums;
using Wavee.UI.Interfaces.ViewModels;
using Wavee.UI.Models.TrackSources;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.ViewModels.Libray
{
    public abstract partial class AbsLibraryViewModel<T> : ObservableObject, ILibraryViewModel, IPlayableViewModel
    {
        private bool _heartedFilter;
        private bool _hasHeartedFilter;
        private readonly string _type;
        public AbsLibraryViewModel()
        {
            _type = this.GetType().Name;
            HeartedFilter = ShellViewModel.Instance.User.ReadPreference<bool>($"{_type}.hearted_only");
        }
        [ObservableProperty]
        private AbsTrackSource<T>? _tracks;

        public bool HeartedFilter
        {
            get => _heartedFilter;
            set
            {
                if (SetProperty(ref _heartedFilter, value))
                {
                    //save user preferences
                    ShellViewModel.Instance.User.SavePreference($"{_type}.hearted_only", value);
                    if (Tracks != null)
                    {
                        Tracks.HeartedFilter = value;
                    }
                }
            }
        }

        public bool HasHeartedFilter
        {
            get => _hasHeartedFilter;
            set => SetProperty(ref _hasHeartedFilter, value);
        }
        public ServiceType? Service
        {
            get; set;
        }

        public abstract Task Initialize();
        public abstract AsyncRelayCommand<TrackViewModel> PlayCommand
        {
            get;
        }
    }
}
