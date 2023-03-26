using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Newtonsoft.Json;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Profiles;

namespace Wavee.UI.ViewModels.User
{
    public partial class UserViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SidebarExpandedAndLargeImageComposite))]
        private bool _sidebarExpanded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SidebarExpandedAndLargeImageComposite))]
        private bool _largeImage;

        [ObservableProperty] private double _sidebarWidth;

        public UserViewModel(Profile profile)
        {
            ForProfile = profile;
            PropertyChanged += OnPropertyChanged;

            SidebarExpanded = profile.SidebarExpanded;
            LargeImage = profile.LargeImage;
            SidebarWidth = Math.Max(100, profile.SidebarWidth);
        }

        public bool SidebarExpandedAndLargeImageComposite => LargeImage && SidebarExpanded;

        public Profile ForProfile
        {
            get => _forProfile;
            private set => SetProperty(ref _forProfile, value);
        }

        public Task SaveTrack(string id)
        {
            ForProfile.SavedTracks.Add(id);
            return SaveData();
        }

        private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_allowedProps.Contains(e.PropertyName))
            {
                ForProfile = ForProfile with
                {
                    SidebarExpanded = SidebarExpanded,
                    LargeImage = LargeImage,
                    SidebarWidth = SidebarWidth,
                };
                await SaveData();
            }
        }

        private async Task SaveData()
        {
            var profileMangager = Ioc.Default.GetRequiredService<IProfileManager>();

            await profileMangager.SaveProfile(ForProfile);
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
            _ = SaveData();
        }

        public T ReadPreference<T>(string key, T defaultval = default)
        {
            if (ForProfile.Properties.TryGetValue(key, out var value))
            {
                //bool, int, string etc
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value);
            }

            return defaultval;
        }

        public async Task SaveJsonPreference<T>(string key, T value)
        {
            try
            {
                ForProfile.Properties[key] = JsonConvert.SerializeObject(value, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });
                await SaveData();
            }
            catch (Exception x)
            {

            }
        }

        public T ReadJsonPreference<T>(string key)
        {
            if (ForProfile.Properties.TryGetValue(key, out var value))
            {
                //bool, int, string etc
                return JsonConvert.DeserializeObject<T>(value,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });
            }

            return default;
        }
    }
}