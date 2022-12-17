using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Eum.UI.Users;
using Eum.UI.ViewModels.Navigation;

namespace Eum.UI.ViewModels.Settings
{
    [INotifyPropertyChanged]
    public sealed partial class SettingsViewModel : INavigatable
    {
        [ObservableProperty] private SettingComponentViewModel _currentlySelectedComponent;
        public SettingsViewModel(EumUser forUser)
        {
            Components = new SettingComponentViewModel[]
            {
                new AppSettingsViewModel(forUser),
                new AudioSettingsViewModel(),
                new EqualizerSettingsViewModel(),
                new StorageSettingsViewModel(),
            };
            CurrentlySelectedComponent = Components.First();
        }
        public SettingComponentViewModel[] Components { get; }
        public void OnNavigatedTo(object parameter)
        {
            
        }

        public void OnNavigatedFrom()
        {
            
        }

        public int MaxDepth => 0;
    }
}
