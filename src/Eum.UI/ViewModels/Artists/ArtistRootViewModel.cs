using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ColorThiefDotNet;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.UI.Helpers;
using Eum.UI.Items;
using Eum.UI.Services.Artists;
using Eum.UI.ViewModels.Navigation;

namespace Eum.UI.ViewModels.Artists
{
    [INotifyPropertyChanged]
    public partial class ArtistRootViewModel : INavigatable, IGlazeablePage
    {
        [ObservableProperty]
        private EumArtist? _artist;

        [ObservableProperty]
        public DiscographyGroup[] _discography;

        [ObservableProperty]
        private IList<TopTrackViewModel> _topTracks;

        [ObservableProperty] 
        private LatestReleaseWrapper? _latestRelease;
        public ItemId Id { get; init; }

        public async void OnNavigatedTo(object parameter)
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
                        TemplateType = a.Key switch
                        {
                            DiscographyType.Album => TemplateTypeOrientation.Grid,
                            DiscographyType.Single => TemplateTypeOrientation.Grid,
                            DiscographyType.Compilation => TemplateTypeOrientation.Grid,
                            DiscographyType.AppearsOn => TemplateTypeOrientation.HorizontalStack,
                            _ => throw new ArgumentOutOfRangeException()
                        },
                        Title = k.Name,
                        Description = k.Year.ToString(),
                        Image = k.Cover.Uri,
                        Tracks = (k.Discs != null
                            ? k.Discs.SelectMany(j => j.Select((z,i) => new DiscographyTrackViewModel
                            {
                                IsLoading = false, 
                                Duration = z.Duration,
                                Index =i ,
                                Title = z.Name,
                                Playcount = (long) z.PlayCount
                            }))
                            : Enumerable.Range(0, (int)k.TrackCount)
                                .Select(_ => new DiscographyTrackViewModel
                                {
                                    IsLoading = true,
                                })).ToArray()
                    }).ToArray(),
                    TemplateType = a.Key switch
                    {
                        DiscographyType.Album => TemplateTypeOrientation.Grid,
                        DiscographyType.Single => TemplateTypeOrientation.Grid,
                        DiscographyType.Compilation => TemplateTypeOrientation.Grid,
                        DiscographyType.AppearsOn => TemplateTypeOrientation.HorizontalStack,
                        _ => throw new ArgumentOutOfRangeException()
                    }
                })
                .Where(a=> a.Items.Any())
                .ToArray();

            TopTracks = artist.TopTrack
                .Select(a => new TopTrackViewModel(a))
                .ToArray();
            LatestRelease = artist.LatestRelease;

        }

        public void OnNavigatedFrom()
        {
            
        }

        public int MaxDepth => 1;
        public bool ShouldSetPageGlaze => Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.ThemeService
            .Glaze == "Page Dependent";

        public async ValueTask<string> GetGlazeColor(CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(Artist.Header))
            {
                using var fs = await Ioc.Default.GetRequiredService<IFileHelper>()
                    .GetStreamForString(Artist.Header, ct);
                using var bmp = new Bitmap(fs);
                var colorThief = new ColorThief();
                var c = colorThief.GetPalette(bmp);

                return c[0].Color.ToHexString();
            }

            return string.Empty;
        }
    }

    [INotifyPropertyChanged]
    public partial class TopTrackViewModel 
    {
        public TopTrackViewModel(ArtistTopTrack track)
        {
            Track = track;
        }

        public ArtistTopTrack Track { get; }

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
            switch (_templateType)
            {
                case TemplateTypeOrientation.Grid:
                    TemplateType = TemplateTypeOrientation.VerticalStack;
                    foreach (var discographyViewModel in Items)
                    {
                        discographyViewModel.TemplateType = TemplateType;
                    }
                    break;
                case TemplateTypeOrientation.VerticalStack:
                    TemplateType = TemplateTypeOrientation.Grid;
                    foreach (var discographyViewModel in Items)
                    {
                        discographyViewModel.TemplateType = TemplateType;
                    }
                    break;
                case TemplateTypeOrientation.HorizontalStack:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string Title => Type.ToString();
        public DiscographyViewModel[] Items { get; set; }
        public bool CanSwitchTemplates => Type is DiscographyType.Album or DiscographyType.Single;
        public DiscographyType Type { get; init; }
        public ICommand SwitchTemplateCommand { get; }
    }
    [INotifyPropertyChanged]
    public partial class DiscographyViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public DiscographyTrackViewModel[] Tracks { get; set; }
        [ObservableProperty]
        private TemplateTypeOrientation _templateType;
    }

    public class DiscographyTrackViewModel
    {
        public bool IsLoading { get; set; }
        public int Index { get; init; }
        public string Title { get; init; }
        public long Playcount { get; init; }
        public int Duration { get; init; }
    }
    public enum TemplateTypeOrientation
    {
        Grid,
        VerticalStack,
        HorizontalStack
    }

}
