using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml.Documents;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GeneralSettingsView : Page
    {
        public GeneralSettingsView()
        {
            this.InitializeComponent();
        }
        public SettingsViewModel<WaveeUIRuntime> ViewModel => SettingsViewModel<WaveeUIRuntime>.Instance;
        private void GoToCache(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            SettingsView.Instance.NavigateToCache();
        }

        public int ToInt(AppTheme appTheme)
        {
            return (int)appTheme;
        }

        private async void ThemeSegment_OnItemClick(object sender, ItemClickEventArgs e)
        {
            await Task.Delay(50);
            ViewModel.CurrentTheme = ((int)ThemeSegment.SelectedIndex) switch
            {
                0 => AppTheme.System,
                1 => AppTheme.Light,
                2 => AppTheme.Dark,
            };
            //change theme
            if (App.MWindow.Content is FrameworkElement f)
            {
                f.RequestedTheme = ViewModel.CurrentTheme switch
                {
                    AppTheme.System => ElementTheme.Default,
                    AppTheme.Light => ElementTheme.Light,
                    AppTheme.Dark => ElementTheme.Dark,
                };
            }
        }

        private void LanguageBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lang = (AppLocale)LanguageBox.SelectedItem;
            ViewModel.CurrentLocale = lang;
        }
    }
}
