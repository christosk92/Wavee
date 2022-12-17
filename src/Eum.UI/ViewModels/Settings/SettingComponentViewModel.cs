using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Eum.UI.ViewModels.Settings
{
    [INotifyPropertyChanged]
    public abstract partial class SettingComponentViewModel
    {
        public abstract string Title { get; }
        public abstract string Icon { get; }
        public abstract string IconFontFamily { get; }
    }
}
