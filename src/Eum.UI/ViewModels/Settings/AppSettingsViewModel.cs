using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Services;
using Eum.UI.Users;

namespace Eum.UI.ViewModels.Settings
{
    public partial class AppSettingsViewModel : SettingComponentViewModel
    {
        [ObservableProperty] 
        private AppTheme _appTheme;


        public AppSettingsViewModel(EumUser forUser)
        {
            AppTheme = forUser.ThemeService.Theme;
            SwitchThemeCommand = new AsyncRelayCommand<AppTheme>(async theme =>
            {
                await forUser.ThemeService.SetThemeAsync(theme);
            });
        }

        public override string Title => "App & Appearance";
        public override string Icon => "\uE771";
        public override string IconFontFamily => "Segoe Fluent Icons";
        public ICommand SwitchThemeCommand { get; }
    }

    public enum AppTheme
    {
        Dark,
        Light,
        SystemDefault
    }
}
