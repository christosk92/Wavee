// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Settings;
using Microsoft.Toolkit.Uwp.Helpers;
using ColorChangedEventArgs = Microsoft.UI.Xaml.Controls.ColorChangedEventArgs;
using ColorPicker = Microsoft.UI.Xaml.Controls.ColorPicker;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Views.Settings
{
    public sealed partial class AppSettingsView : UserControl
    {
        public AppSettingsViewModel ViewModel { get; }
        public AppSettingsView(AppSettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        private void ColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            ViewModel.Glaze = args.NewColor.ToHex();
        }
    }
}
