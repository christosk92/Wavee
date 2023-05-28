using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Settings
{
    public sealed partial class SettingsView :
        UserControl, INavigablePage
    {

        public SettingsView()
        {
            this.InitializeComponent();
            Instance = this;
        }

        public bool ShouldKeepInCache(int depth)
        {
            //nah fam
            return false;
        }

        Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

        public void NavigatedTo(object parameter)
        {
            //ok so what?
        }

        public void RemovedFromCache()
        {
            //nothing!
            Instance = null;
        }

        public SettingsViewModel<WaveeUIRuntime> ViewModel
            => SettingsViewModel<WaveeUIRuntime>.Instance;

        private void NavVi_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            switch (args.InvokedItemContainer.Tag)
            {
                case "general":
                    MainSettingsFrame.Navigate(typeof(GeneralSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
                case "remote":
                    MainSettingsFrame.Navigate(typeof(RemoteSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
                case "playback":
                    MainSettingsFrame.Navigate(typeof(PlaybackSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
                case "equalizer":
                    MainSettingsFrame.Navigate(typeof(EqualizerSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
                case "cache":
                    MainSettingsFrame.Navigate(typeof(CacheSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
            }
        }

        public static SettingsView Instance { get; private set; }

        public void NavigateToCache()
        {
            MainSettingsFrame.Navigate(typeof(CacheSettingsView), null, new EntranceNavigationTransitionInfo());
        }

        private void MainSettingsFrame_OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            var to = e.SourcePageType;
            switch (to)
            {
                case var _ when to == typeof(GeneralSettingsView):
                    NavVi.SelectedItem = NavVi.MenuItems[0];
                    Component_HeaderText.Text = "General";
                    Component_SubtitleText.Text = "Personalization settings such as language, themes and notifications";
                    break;
                case var _ when to == typeof(RemoteSettingsView):
                    NavVi.SelectedItem = NavVi.MenuItems[1];
                    Component_HeaderText.Text = "Remote";
                    Component_SubtitleText.Text = "Settings related to Spotify Remote.";
                    break;
                case var _ when to == typeof(PlaybackSettingsView):
                    NavVi.SelectedItem = NavVi.MenuItems[2];
                    Component_HeaderText.Text = "Playback";
                    Component_SubtitleText.Text = "Settings related to playback such as crossfading and audio quality.";
                    break;
                case var _ when to == typeof(EqualizerSettingsView):
                    NavVi.SelectedItem = NavVi.MenuItems[3];
                    Component_HeaderText.Text = "Equalizer";
                    Component_SubtitleText.Text = "Settings related to the equalizer.";
                    break;
                case var _ when to == typeof(CacheSettingsView):
                    NavVi.SelectedItem = NavVi.MenuItems[4];
                    Component_HeaderText.Text = "Cache";
                    Component_SubtitleText.Text = "Wavee may cache tracks and/or audio files for a faster experience.";
                    break;
            }
        }
    }
}
