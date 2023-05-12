using System.Reactive.Disposables;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Wavee.UI.ViewModels.Playlist;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using ReactiveUI;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Playlists
{
    public sealed partial class CreatePlaylistView : UserControl, IViewFor<CreatePlaylistViewModel>
    {
        public CreatePlaylistView()
        {
            this.InitializeComponent();
            ViewModel = new CreatePlaylistViewModel();

            this.WhenActivated(disposable =>
            {
                this.Bind(ViewModel,
                        x => x.PlaylistName,
                        x => x.PlaylistTitleBox.Text)
                    .DisposeWith(disposable);

                this.Bind(ViewModel,
                        x => x.CreateInSpotify,
                        x => x.CreateInSpotifyBox.IsChecked)
                    .DisposeWith(disposable);

                this.BindCommand(ViewModel,
                        x => x.CreateCommand,
                        x => x.CreateButton)
                    .DisposeWith(disposable);

                MainViewModel.Instance.UserIsLoggedIn
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(c => c ? Visibility.Collapsed : Visibility.Visible).BindTo(this, x => x.WhyDisabledSync.Visibility);

                MainViewModel.Instance.UserIsLoggedIn
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .BindTo(this, x => x.CreateInSpotifyControl.IsEnabled);
            });
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (CreatePlaylistViewModel)value;
        }

        public CreatePlaylistViewModel ViewModel { get; set; }

        private void WhyDisabledSync_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ExplainWhyYouCannotSyncTooltip.IsOpen = true;
        }
    }
}
