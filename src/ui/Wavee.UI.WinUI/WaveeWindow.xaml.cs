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
using Wavee.UI.User;
using Wavee.UI.ViewModel;
using Wavee.UI.ViewModel.Wizard;
using Wavee.UI.WinUI.Dialogs;
using Wavee.UI.WinUI.Providers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;
using static Org.BouncyCastle.Math.EC.ECCurve;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WaveeWindow : WindowEx
    {
        public WaveeWindow()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();
            var appWindow = this.AppWindow;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

            var globalPath = AppProviders.GetPersistentStoragePath();
            var globalSettings = LoadOrCreateUiConfig(globalPath);
            ViewModel = new MainWindowViewModel(globalSettings);
            ViewModel.PropertyChanged += ViewModel_PropertyChangedAsync;
        }

        private async void ViewModel_PropertyChangedAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.CurrentView))
            {
                if (ViewModel.CurrentView is WizardViewModel wizard)
                {
                    await ShowWizard(wizard);
                }
                else
                {
                    this.Content = (UIElement)ViewFactory.ConstructFromViewModel(ViewModel.CurrentView);
                }
            }
        }

        private async Task ShowWizard(WizardViewModel wizard)
        {
            var dialog = new WizardDialog(wizard)
            {
                Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"]
            };
            dialog.XamlRoot = this.Content.XamlRoot;
            dialog.Closing += (s, e) =>
            {
                //only close if the wizard is done
                if (wizard.IsDone)
                {
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
            };
            await dialog.ShowAsync();
        }

        public MainWindowViewModel ViewModel { get; }


        private static GlobalSettings LoadOrCreateUiConfig(string dataDir)
        {
            Directory.CreateDirectory(dataDir);

            var uiConfig = new GlobalSettings(Path.Combine(dataDir, "Global.json"));
            uiConfig.LoadFile(createIfMissing: true);

            return uiConfig;
        }

        private async void Dummy_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.Initialize();
        }
    }
}
