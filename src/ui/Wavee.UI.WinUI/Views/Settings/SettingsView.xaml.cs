using LanguageExt;
using Microsoft.UI.Xaml.Controls;
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
        }

        public SettingsViewModel<WaveeUIRuntime> ViewModel
            => SettingsViewModel<WaveeUIRuntime>.Instance;

        private void NavVi_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            switch (args.InvokedItemContainer.Tag)
            {
                case "general":
                    Component_HeaderText.Text = "General";
                    Component_SubtitleText.Text = "Personalization settings such as language, themes and notifications";
                    MainSettingsFrame.Navigate(typeof(GeneralSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
                case "remote":
                    Component_HeaderText.Text = "Remote";
                    Component_SubtitleText.Text = "Settings related to Spotify Remote.";
                    MainSettingsFrame.Navigate(typeof(RemoteSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
                case "playback":
                    Component_HeaderText.Text = "Playback";
                    Component_SubtitleText.Text = "Settings related to playback such as crossfading and audio quality.";
                    MainSettingsFrame.Navigate(typeof(PlaybackSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
                case "equalizer":
                    Component_HeaderText.Text = "Equalizer";
                    Component_SubtitleText.Text = "Settings related to the equalizer.";
                    MainSettingsFrame.Navigate(typeof(EqualizerSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
                case "cache":
                    Component_HeaderText.Text = "Cache";
                    Component_SubtitleText.Text = "Wavee may cache tracks and/or audio files for a faster experience.";
                    MainSettingsFrame.Navigate(typeof(CacheSettingsView), null, args.RecommendedNavigationTransitionInfo);
                    break;
            }
        }
    }
}
