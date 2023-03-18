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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.UI.ViewModels.Login.Impl;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.WinUI.Helpers.DragDropHelpers;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Login
{
    public sealed partial class CreateLocalProfileView : UserControl
    {
        public CreateLocalProfileView()
        {
            this.InitializeComponent();
        }

        public CreateLocalProfileViewModel ViewModel => this.DataContext as CreateLocalProfileViewModel;

        private async void OpenBrowsePictureDialogTapped(object sender, TappedRoutedEventArgs e)
        {
            var filePicker = App.MainWindow
                .CreateOpenFilePicker();

            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");

            var picker = await filePicker.PickSingleFileAsync();
            if (picker?.Path != null)
            {
                ViewModel.ProfilePicture = await Ioc.Default.GetRequiredService<IFileService>()
                    .CopyToAppStorage(picker!.Path);
            }
        }

        private async void Picture_Dropped(object sender, DragEventArgs e)
        {
            var picture = await e.GetStorageItemAsync();
            if (picture != null)
            {
                //Copy 
                ViewModel.ProfilePicture = await Ioc.Default.GetRequiredService<IFileService>()
                    .CopyToAppStorage(picture.Path);
            }
        }

        private void Picture_DragOver(object sender, DragEventArgs e)
        {
            e.AllowDrag("Use image", DataPackageOperation.Copy);
        }
    }
}
