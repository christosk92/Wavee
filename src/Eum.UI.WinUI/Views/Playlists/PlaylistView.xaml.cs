// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Eum.UI.ViewModels.Playlists;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Playlists
{
    public sealed partial class PlaylistView : UserControl
    {
        public PlaylistView(PlaylistViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }


        public PlaylistViewModel ViewModel { get; }

        private void TrackList_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = "Add track to playlist"; // Sets custom UI text
            e.DragUIOverride.IsCaptionVisible = true; // Sets if the caption is visible
            e.DragUIOverride.IsContentVisible = true; // Sets if the dragged content is visible
            e.DragUIOverride.IsGlyphVisible = true; // Sets if the glyph is visibile
        }

        private async void TrackList_OnDrop(object sender, DragEventArgs e)
        {
            // if (e.DataView.Contains(StandardDataFormats.StorageItems))
            // {
            //     var items = await e.DataView.GetStorageItemsAsync();
            //     if (items.Count > 0)
            //     {
            //         var tracksRepo = Ioc.Default.GetRequiredService<TracksRepository>();
            //         var storageFiles = items;
            //         foreach (var storageFile in storageFiles)
            //         {
            //             switch (storageFile)
            //             {
            //                 case StorageFolder folder:
            //                     break;
            //                 case StorageFile file:
            //                 {
            //                     // Set the image on the main page to the dropped image
            //                     if (file.ContentType.StartsWith("audio/"))
            //                     {
            //                         var vm = file.Create();
            //                         ViewModel.AddTrack(vm);
            //                         tracksRepo.UpsertTrack(new TrackCacheEntity
            //                         {
            //                             Album = vm.Album,
            //                             Artists = vm.Artists,
            //                             Duration = vm.Duration,
            //                             Id = vm.Id,
            //                             Image = vm.Image,
            //                             Title = vm.Title
            //                         });
            //                     }
            //
            //                     break;
            //                 }
            //             }
            //         }
            //     }
            // }
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
                    if (storageFile.ContentType.StartsWith("image/"))
                    {
                        //var bitmapImage = new BitmapImage();
                        var copyToApplicationData = await storageFile.CopyAsync(ApplicationData.Current.LocalFolder,
                            Guid.NewGuid().ToString());
                        // bitmapImage.DecodePixelHeight = 192;
                        // bitmapImage.DecodePixelWidth = 192;
                        // bitmapImage.DecodePixelType = DecodePixelType.Logical;
                        // bitmapImage.SetSource(await copyToApplicationData.OpenAsync(FileAccessMode.Read));
                        // // Set the image on the main page to the dropped image
                        // var imageBrush = new ImageBrush
                        // {
                        //     ImageSource = bitmapImage,
                        //     Stretch = Stretch.UniformToFill
                        // };
                        ViewModel.Playlist.ImagePath = copyToApplicationData.Path;
                    }
                }
            }
        }

      
    }

}
