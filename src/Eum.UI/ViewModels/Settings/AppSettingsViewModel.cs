using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using Eum.UI.Services;
using Eum.UI.Users;
using ReactiveUI;

namespace Eum.UI.ViewModels.Settings
{
    public partial class AppSettingsViewModel : SettingComponentViewModel
    {
        [ObservableProperty] 
        private AppTheme _appTheme;
        [ObservableProperty]
        private string _selectedGlaze;

        [NotifyPropertyChangedFor(nameof(GlazeIsCustomColor))]
        [ObservableProperty]
        private string _glaze;
        // [ObservableProperty]
        // private string _accentColor; ;
        public AppSettingsViewModel(EumUser forUser)
        {
            Glaze = forUser.Accent;
            AppTheme = forUser.ThemeService.Theme;
            switch (forUser.Accent)
            {
                case "System Color":
                    SelectedGlaze = GlazeSelections[0];
                    break;
                case "Page Dependent":
                    SelectedGlaze = GlazeSelections[1];
                    break;
                case "Playback Dependent":
                    SelectedGlaze = GlazeSelections[2];
                    break;
                default:
                    SelectedGlaze = GlazeSelections.Last();
                    break;
            }
            //AccentColor = forUser.ThemeService.Accent;
            SwitchThemeCommand = new RelayCommand<int>(theme =>
            {
                forUser.ThemeService.SetTheme((AppTheme)theme);
                AppTheme = (AppTheme) theme;
            });
            this.WhenPropertyChanged(model => model.Glaze)
                .Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(value =>
                {
                    if (value.Value is "Custom Color")
                    {
                        forUser.ThemeService.SetGlaze(value.Value ?? forUser.LastAccent);
                    }
                    else
                    {
                        forUser.ThemeService.SetGlaze(value.Value);
                    }
                });

            this.WhenPropertyChanged(model => model.SelectedGlaze)
                .Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(value =>
                {
                    if (value.Value is "Custom Color")
                    {
                        forUser.ThemeService.SetGlaze(forUser.LastAccent);
                    }
                    else
                    {
                        forUser.ThemeService.SetGlaze(value.Value);
                    }
                    Glaze = forUser.Accent;
                });
        }
        public bool GlazeIsCustomColor => Glaze?.StartsWith("#") ?? true;
     
        public override string Title => "App & Appearance";
        public override string Icon => "\uE771";
        public override string IconFontFamily => "Segoe Fluent Icons";
        public ICommand SwitchThemeCommand { get; }

        public IList<string> GlazeSelections => new List<string>
        {
            "System Color",
            "Page Dependent",
            "Playback Dependent",
            "Custom Color"
        };
        
    }

    public enum AppTheme : int
    {
        Dark = 0,
        Light = 1,
        SystemDefault = 2
    }
}
