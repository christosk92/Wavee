// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Eum.UI.ViewModels.Playlists;
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
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Playlists
{
    public sealed partial class CreatePlaylistView : UserControl
    {
        public CreatePlaylistView(CreatePlaylistViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        public CreatePlaylistViewModel ViewModel { get; }

        private void WhyDisabledSync_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleThemeTeachingTip1.IsOpen = true;
        }

        private void Image_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drop image"; // Sets custom UI text
            e.DragUIOverride.IsCaptionVisible = true; // Sets if the caption is visible
            e.DragUIOverride.IsContentVisible = true; // Sets if the dragged content is visible
            e.DragUIOverride.IsGlyphVisible = true; // Sets if the glyph is visibile

        }

        private async void IMage_drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageFile = items[0] as StorageFile;
                    // Set the image on the main page to the dropped image
                    if (!storageFile.ContentType.StartsWith("image/"))
                    {
                        //show infobar with error:
                        this.InfoBar.Title = "Invalid image. Valid types are: jpeg, png.";
                        this.InfoBar.Severity = InfoBarSeverity.Error;
                        this.InfoBar.IsOpen = true;
                    }
                    else
                    {
                        this.InfoBar.IsOpen = false;
                        var bitmapImage = new BitmapImage();
                        var copyToApplicationData = await storageFile.CopyAsync(ApplicationData.Current.LocalFolder, Guid.NewGuid().ToString());
                        bitmapImage.SetSource(await copyToApplicationData.OpenAsync(FileAccessMode.Read));
                        // Set the image on the main page to the dropped image
                        Img.Source = bitmapImage;
                        ViewModel.SelectedImage = copyToApplicationData.Path;
                    }
                }
            }
        }
    }
}
