// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eum.Connections.Spotify.Models.Artists;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.UI.Models.Serialization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView_Test : UserControl
    {
        public ShellView_Test()
        {
            using var buble = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets/buble.json"));
            var jsonDeserialized = JsonSerializer.Deserialize<MercuryArtist>(buble, JsonSerializerOptions.Default);
            Discography = jsonDeserialized.DiscographyReleases
                .Select(a => new DiscographyGroup
                {
                    Type = a.Key,
                    Items = a.Value.Select(k => new DiscographyViewModel
                    {
                        Title = k.Name,
                        Description = k.Year.ToString(),
                        Image = k.Cover.Uri,
                        Tracks = (k.Discs != null
                            ? k.Discs.SelectMany(j => j.Select(z => new DiscographyTrackViewModel
                            {
                                IsLoading = false
                            }))
                            : Enumerable.Range(0, (int)k.TrackCount)
                                .Select(_ => new DiscographyTrackViewModel
                                {
                                    IsLoading = true
                                })).ToArray()
                    }).ToArray()
                })
                .ToArray();
            this.InitializeComponent();
        }
        public DiscographyGroup[] Discography { get; }
    }

    [INotifyPropertyChanged]
    public partial class DiscographyGroup
    {
        [ObservableProperty]
        private TemplateTypeOrientation _templateType;
        public DiscographyGroup()
        {
            SwitchTemplateCommand = new RelayCommand<NavigationViewItemInvokedEventArgs>(SwitchTemplates);
        }

        private void SwitchTemplates(NavigationViewItemInvokedEventArgs obj)
        {
            switch (obj.InvokedItemContainer.Tag)
            {
                case "0":
                    TemplateType = TemplateTypeOrientation.Grid;
                    break;
                case "1":
                    TemplateType = TemplateTypeOrientation.VerticalStack;
                    break;
            }
        }

        public string Title => Type.ToString();
        public DiscographyViewModel[] Items { get; set; }
        public bool CanSwitchTemplates => Type is DiscographyType.Album or DiscographyType.Single;
        public DiscographyType Type { get; set; }
        public ICommand SwitchTemplateCommand { get; }
    }

    public enum TemplateTypeOrientation 
    {
        Grid,
        VerticalStack
    }

    public class DiscographyViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public DiscographyTrackViewModel[] Tracks { get; set; }
    }

    public class DiscographyTrackViewModel
    {
        public bool IsLoading { get; set; }
    }
}
