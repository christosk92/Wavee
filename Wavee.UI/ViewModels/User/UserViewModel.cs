using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Profiles;

namespace Wavee.UI.ViewModels.User
{
    public partial class UserViewModel : ObservableObject
    {
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(SidebarExpandedAndLargeImageComposite))]
        private bool _sidebarExpanded;

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(SidebarExpandedAndLargeImageComposite))]
        private bool _largeImage;

        [ObservableProperty] private double _sidebarWidth;

        public UserViewModel(Profile profile)
        {
            ForProfile = profile;
            PropertyChanged += OnPropertyChanged;

            SidebarExpanded = profile.SidebarExpanded;
            LargeImage = profile.LargeImage;
            SidebarWidth = profile.SidebarWidth;
        }

        public bool SidebarExpandedAndLargeImageComposite => LargeImage && SidebarExpanded;

        public Profile ForProfile
        {
            get => _forProfile;
            private set => SetProperty(ref _forProfile, value);
        }

        public void SaveTrack(string id)
        {
            ForProfile.SavedTracks.Add(id);
            SaveData();
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_allowedProps.Contains(e.PropertyName))
            {
                ForProfile = ForProfile with
                {
                    SidebarExpanded = SidebarExpanded,
                    LargeImage = LargeImage,
                    SidebarWidth = SidebarWidth,
                };
                SaveData();
            }
        }

        private void SaveData()
        {
            var profileMangager = Ioc.Default.GetRequiredService<IProfileManager>();

            profileMangager.SaveProfile(ForProfile);
        }

        private static string[] _allowedProps = new[]
        {
            nameof(SidebarWidth),
            nameof(LargeImage),
            nameof(SidebarExpanded)
        };

        private Profile _forProfile;

        public void SavePreference<T>(string key, T value)
        {
            ForProfile.Properties[key] = value.ToString();
            SaveData();
        }

        public T ReadPreference<T>(string key)
        {
            if (ForProfile.Properties.TryGetValue(key, out var value))
            {
                //bool, int, string etc
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value);
            }

            return default;
        }
    }
}