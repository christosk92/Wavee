using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.Enums;
using Wavee.UI.Helpers.Extensions;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Interfaces.ViewModels;
using Wavee.UI.Models.Navigation;
using Wavee.UI.ViewModels.Libray;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.ViewModels.Home
{
    public partial class LocalHomeViewModel : ObservableObject, INavigationAware
    {
        private readonly ILocalAudioDb _db;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly INavigationService _navigationService;
        public LocalHomeViewModel(ILocalAudioDb db, IStringLocalizer stringLocalizer, INavigationService navigationService)
        {
            _db = db;
            _stringLocalizer = stringLocalizer;
            _navigationService = navigationService;
        }

        public ObservableGroupedCollection<string, TrackViewModel> LatestFiles { get; } = new();

        [RelayCommand]
        public void SeeAll()
        {
            _navigationService.NavigateTo(typeof(LibraryRootViewModel).FullName,
                new LibraryNavigationParameters(nameof(LibrarySongsViewModel), false, ServiceType.Local),
                false, AnimationType.SlideRTL);
        }

        public async void OnNavigatedTo(object parameter)
        {
            const double minSecondsDiff = 60 * 60 * 24;
            var latestImports = (await _db.GetLatestImportsAsync(20))
                .GroupBy(track => track.DateImported.CalculateRelativeDateString(minSecondsDiff, _stringLocalizer));

            var depth = 0;
            foreach (var latestImport in latestImports)
            {
                var tracksProjected = latestImport.Select((track, index) => new TrackViewModel(track,
                    index + depth,
                    PlaybackViewModel.Instance.PlayTaskCommand))
                    .ToArray();

                depth += tracksProjected.Length;

                LatestFiles.Add(new ObservableGroup<string, TrackViewModel>(latestImport.Key, tracksProjected));
            }

        }

        public void OnNavigatedFrom()
        {
        }

    }
}
