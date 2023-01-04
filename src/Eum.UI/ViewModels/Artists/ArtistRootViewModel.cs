using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Eum.UI.Services.Albums;
using Eum.UI.Services.Artists;
using Eum.UI.ViewModels.Navigation;
using Nito.AsyncEx;
using ReactiveUI;
using System.Reactive.Concurrency;
using Eum.UI.ViewModels.Playlists;
using AsyncLock = Nito.AsyncEx.AsyncLock;
using Eum.UI.ViewModels.Settings;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Clients;

namespace Eum.UI.ViewModels.Artists
{
    [INotifyPropertyChanged]
    public partial class ArtistRootViewModel : INavigatable, IGlazeablePage
    {
        [ObservableProperty] private EumArtist? _artist;

        [ObservableProperty] public DiscographyGroup[]? _discography;

        [ObservableProperty] private IList<TopTrackViewModel> _topTracks;

        [ObservableProperty] private LatestReleaseWrapper? _latestRelease;
        public ItemId Id { get; init; }

        private static readonly RelayCommand<DiscographyGroup> _switchTemplatesCommand =
            new RelayCommand<DiscographyGroup>(SwitchTemplates);

        private static void SwitchTemplates(DiscographyGroup gro)
        {
            switch (gro.TemplateType)
            {
                case TemplateTypeOrientation.Grid:
                    gro.TemplateType = TemplateTypeOrientation.VerticalStack;
                    break;
                case TemplateTypeOrientation.VerticalStack:
                    gro.TemplateType = TemplateTypeOrientation.Grid;
                    break;
                case TemplateTypeOrientation.HorizontalStack:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async void OnNavigatedTo(object parameter)
        {
            var provider = Ioc.Default.GetRequiredService<IArtistProvider>();

            var artist = await Task.Run(async () => await provider.GetArtist(Id, "en_us"));
            Artist = artist;
            Discography = artist.DiscographyReleases
                .Select(a => new DiscographyGroup
                {
                    Type = a.Key,
                    SwitchTemplateCommand = _switchTemplatesCommand,
                    Items = a.Value.Select(k => new DiscographyViewModel
                    {
                        Title = k.Name,
                        Description = k.Year.ToString(),
                        Image = k.Cover.Uri,
                        Id = new ItemId(k.Uri.Uri),
                        Tracks = (k.Discs != null
                            ? k.Discs.SelectMany(j => j.Select((z, i) => new DiscographyTrackViewModel
                            {
                                IsLoading = false,
                                Id = new ItemId(z.Uri.Uri),
                                Duration = z.Duration,
                                Index = i,
                                Title = z.Name,
                                Playcount = (long) z.PlayCount
                            }))
                            : Enumerable.Range(0, (int) k.TrackCount)
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
                .Where(a => a.Items.Any())
                .ToArray();

            TopTracks = artist.TopTrack
                .Select(a => new TopTrackViewModel(a))
                .ToArray();
            LatestRelease = artist.LatestRelease;
            _waitForArtist.Set();
        }

        public async void OnNavigatedFrom()
        {
            foreach (var discographyGroup in Discography ?? Array.Empty<DiscographyGroup>())
            {
                foreach (var discographyGroupItem in discographyGroup.Items)
                {
                    discographyGroupItem.Cancel();
                    discographyGroupItem.Tracks = null;
                }

                discographyGroup.Items = null;
            }

            Discography = null;
            foreach (var topTrackViewModel in TopTracks)
            {
                foreach (var cachedImage in topTrackViewModel.Track.Track.Images)
                {
                    var image = await cachedImage.ImageStream;
                    image.Dispose();
                }
            }

            TopTracks = new List<TopTrackViewModel>();
        }

        public int MaxDepth => 1;

        public bool ShouldSetPageGlaze => Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.ThemeService
            .Glaze == "Page Dependent";

        private readonly AsyncManualResetEvent _waitForArtist = new();

        public async ValueTask<string> GetGlazeColor(
            AppTheme theme, CancellationToken ct = default)
        {
            await _waitForArtist.WaitAsync(ct);
            if (!string.IsNullOrEmpty(Artist.Header))
            {
                try
                {
                    var colorsClient = Ioc.Default.GetRequiredService<IExtractedColorsClient>();
                    var uri = new Uri(Artist.Header);
                    var color = await
                        colorsClient.GetColors(uri.ToString(), ct);
                    switch (theme)
                    {
                        case AppTheme.Dark:
                            return color[ColorTheme.Dark];
                        case AppTheme.Light:
                            return color[ColorTheme.Light];
                        default:
                            throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
                    }
                }
                catch (Exception ex)
                {
                    using var fs = await Ioc.Default.GetRequiredService<IFileHelper>()
                        .GetStreamForString(Artist.Header, ct);
                    using var bmp = new Bitmap(fs);
                    var colorThief = new ColorThief();
                    var c = colorThief.GetPalette(bmp);

                    return c[0].Color.ToHexString();
                }
            }

            return string.Empty;
        }
    }

    [INotifyPropertyChanged]
    public partial class TopTrackViewModel : IIsPlaying
    {
        public TopTrackViewModel(ArtistTopTrack track)
        {
            Track = track;
        }

        public ArtistTopTrack Track { get; }

        public ItemId Id => Track.Track.Id;

        public bool IsPlaying()
        {
            return Ioc.Default.GetRequiredService<MainViewModel>()
                .PlaybackViewModel?.Item?.Id == Id;
        }

        public bool WasPlaying => _wasPlaying;

        private bool _wasPlaying;

        public event EventHandler<bool>? IsPlayingChanged;

        public void ChangeIsPlaying(bool isPlaying)
        {
            if (_wasPlaying == isPlaying) return;

            _wasPlaying = isPlaying;
            IsPlayingChanged?.Invoke(this, isPlaying);
        }
    }

    [INotifyPropertyChanged]
    public partial class DiscographyGroup
    {
        [ObservableProperty] private TemplateTypeOrientation _templateType;

        public string Title => Type.ToString();
        public DiscographyViewModel[] Items { get; set; }
        public bool CanSwitchTemplates => Type is DiscographyType.Album or DiscographyType.Single;
        public DiscographyType Type { get; init; }
        public ICommand SwitchTemplateCommand { get; set; }
    }

    [INotifyPropertyChanged]
    public partial class DiscographyViewModel
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public ItemId Id { get; init; }

        public DiscographyTrackViewModel[] Tracks
        {
            get
            {
                _ = FetchIfNeccesary();
                return _tracks;
            }
            set
            {
                _tracks = value;
                OnPropertyChanged(nameof(Tracks));
            }
        }

        // private readonly AsyncLock _l = new AsyncLock();
        private async Task FetchIfNeccesary()
        {
            // using (await _l.LockAsync(_cts.Token))
            // {
            if (_tracks.Any(a => !a.IsLoading)) return;

            //todo fetch:
            Debug.WriteLine($"{Title} is missing tracks. Fetching...");
            var album = await Ioc.Default.GetRequiredService<IAlbumProvider>()
                .GetAlbum(Id, "en", _cts.Token);
            IDisposable r = default;
            r = RxApp.MainThreadScheduler.Schedule(() =>
            {
                Tracks = album.Discs.SelectMany(a => a)
                    .Select((z, i) => new DiscographyTrackViewModel
                    {
                        IsLoading = false,
                        Duration = z.Duration,
                        Index = i,
                        Id = new ItemId(z.Uri.Uri),
                        Title = z.Name,
                        Playcount = (long) z.PlayCount
                    }).ToArray();
                r?.Dispose();
            });
            //}
        }

        private DiscographyTrackViewModel[] _tracks;

        public void Cancel()
        {
            try
            {
                _cts.Dispose();
                _cts = null;
            }
            catch (Exception)
            {
            }
        }
    }

    [ObservableObject]
    public partial class DiscographyTrackViewModel : IIsPlaying
    {
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _title;
        [ObservableProperty] private long _playcount;
        [ObservableProperty] private int _duration;
        public int Index { get; init; }
        public ItemId Id { get; init; }

        public bool IsPlaying()
        {
            return Ioc.Default.GetRequiredService<MainViewModel>()
                .PlaybackViewModel?.Item?.Id == Id;
        }

        public bool WasPlaying => _wasPlaying;

        private bool _wasPlaying;

        public event EventHandler<bool>? IsPlayingChanged;

        public void ChangeIsPlaying(bool isPlaying)
        {
            if (_wasPlaying == isPlaying) return;

            _wasPlaying = isPlaying;
            IsPlayingChanged?.Invoke(this, isPlaying);
        }
    }

    public enum TemplateTypeOrientation
    {
        Grid,
        VerticalStack,
        HorizontalStack
    }
}