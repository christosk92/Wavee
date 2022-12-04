using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.UI.Items;
using Eum.UI.Services.Artists;
using Eum.UI.ViewModels.Navigation;

namespace Eum.UI.ViewModels.Artists
{
    [INotifyPropertyChanged]
    public partial class ArtistRootViewModel : INavigatable
    {
        [ObservableProperty]
        private EumArtist? _artist;

        [ObservableProperty]
        public DiscographyGroup[] _discography;
        public ItemId Id { get; init; }

        public async void OnNavigatedTo(bool isInHistory)
        {
            var provider = Ioc.Default.GetRequiredService<IArtistProvider>();

            var artist = await Task.Run(async () => await provider.GetArtist(Id, "en_us"));
            Artist = artist;
            Discography = artist.DiscographyReleases
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
        }

        public void OnNavigatedFrom(bool isInHistory)
        {

        }

        public bool IsActive { get; set; }
    }
    [INotifyPropertyChanged]
    public partial class DiscographyGroup
    {
        [ObservableProperty]
        private TemplateTypeOrientation _templateType;
        public DiscographyGroup()
        {
            SwitchTemplateCommand = new RelayCommand(SwitchTemplates);
        }

        private void SwitchTemplates()
        {
            TemplateType = (TemplateTypeOrientation)(((int)_templateType + 1) % 2);
        }

        public string Title => Type.ToString();
        public DiscographyViewModel[] Items { get; set; }
        public bool CanSwitchTemplates => Type is DiscographyType.Album or DiscographyType.Single;
        public DiscographyType Type { get; set; }
        public ICommand SwitchTemplateCommand { get; }
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
    public enum TemplateTypeOrientation
    {
        Grid,
        VerticalStack
    }

}
