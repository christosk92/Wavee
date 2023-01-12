using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Text.Json;
using Eum.UI.ViewModels.Playlists;
using AsyncLock = Nito.AsyncEx.AsyncLock;
using Eum.UI.ViewModels.Settings;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback;
using Eum.Enums;
using Eum.UI.Services;
using Eum.UI.Services.Library;
using Eum.UI.Services.Tracks;
using Eum.UI.ViewModels.Playback;
using Flurl;
using Flurl.Http;
using Eum.Users;

namespace Eum.UI.ViewModels.Artists
{
    [INotifyPropertyChanged]
    public partial class ArtistRootViewModel : INavigatable, IGlazeablePage, IIsSaved
    {
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isSaved;
        [ObservableProperty] private string? _header;
        [ObservableProperty] private EumArtist? _artist;

        public ObservableCollection<DiscographyGroup> Discography { get; private set; } = new();

        [ObservableProperty] private List<TopTrackViewModel> _topTracks;

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

        public event EventHandler ArtistFetched;
        public async void OnNavigatedTo(object parameter)
        {
            var main = Ioc.Default.GetRequiredService<MainViewModel>();

            var user = main.CurrentUser.User;
            var playback = main.PlaybackViewModel;

            playback.PlayingItemChanged += PlaybackOnPlayingItemChanged;
            PlaybackOnPlayingItemChanged(playback, playback.Item.Id);

            RecheckIsSaved();
            user.LibraryProvider.CollectionUpdated += LibraryProviderOnCollectionUpdated;
            var provider = Ioc.Default.GetRequiredService<IArtistProvider>();
            var artist = await Task.Run(async () => await provider.GetArtist(Id, "en_us"));
            var playCommand = new AsyncRelayCommand<int>(async index =>
            {
                try
                {
                    await Ioc.Default.GetRequiredService<IPlaybackService>()
                        .PlayOnDevice(new PlainContextPlayCommand(Id, index, new Dictionary<string, string>()));
                }
                catch (Exception notImplementedException)
                {
                    await Ioc.Default.GetRequiredService<IErrorMessageShower>()
                        .ShowErrorAsync(notImplementedException, "Unexpected error", "Something went wrong while trying to play the track.");
                }
            });
            Artist = artist;
            Header = artist.Header;
            foreach (var discographyGroup in artist.DiscographyReleases
                         .Select(a => new DiscographyGroup
                         {
                             Key = a.Key switch
                             {
                                 DiscographyType.Album => "Albums",
                                 DiscographyType.Single => "Singles",
                                 DiscographyType.Compilation => "Compilations",
                                 DiscographyType.AppearsOn => "Found On"
                             },
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
                                         IsSaved = user.LibraryProvider.IsSaved(new ItemId(z.Uri.Uri)),
                                         Playcount = (long)z.PlayCount
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
                         .Where(a => a.Items.Any()))
            {
                Discography.Add(discographyGroup);
            }

            TopTracks = artist.TopTrack
                .Select(a => new TopTrackViewModel(a, playCommand)
                {
                    IsSaved = user.LibraryProvider.IsSaved(a.Track.Id)
                })
                .ToList();
            LatestRelease = artist.LatestRelease;
            _waitForArtist.Set();
            ArtistFetched?.Invoke(this, EventArgs.Empty);
            var appleMusicArtist = await Task.Run(async () => await FetchAppleMusicArtist(artist.Name));
            if (appleMusicArtist != null)
            {
                if (Header == null)
                {
                    Header = appleMusicArtist.Value.Header;
                }

                if (appleMusicArtist.Value.featuredAlbums != null)
                {
                    Discography.Insert(0, new DiscographyGroup
                    {
                        TemplateType = TemplateTypeOrientation.Grid,
                        CanSwitchTemplatesOverride = false,
                        Items = appleMusicArtist.Value.
                            featuredAlbums.Value.Albums
                            .Select(a => new DiscographyViewModel
                            {
                                Title = a.Name,
                                Description = string.Join(", ", a.Artists
                                    .Select(a => a.Title)),
                                Id = a.Id,
                                Image = a.Images.First().Id
                                    .Replace("{w}", "250").Replace("{h}", "250"),
                            }).ToArray(),
                        Key = appleMusicArtist.Value.featuredAlbums.Value.Title
                    });
                }
            }

            // if (artist.Header == null)
            // {
            //     //fetch from own api
            //     var header = await Task.Run(async () => await FetchExternalHeader(artist.Name));
            //     if (header != null)
            //     {
            //         Header = header;
            //     }
            // }
        }

        private void LibraryProviderOnCollectionUpdated(object? sender, (EntityType Type, IReadOnlyList<CollectionUpdateNotification> Ids) e)
        {
            if (e.Type is EntityType.Artist or EntityType.Track)
            {
                foreach (var topTrackViewModel in TopTracks ?? new List<TopTrackViewModel>(0))
                {
                    var updatedOrNahh = e.Ids.FirstOrDefault(a => a.Id.Id.Uri == topTrackViewModel.Id.Uri);
                    if (updatedOrNahh != null)
                    {
                        topTrackViewModel.IsSaved = updatedOrNahh.Added;
                    }
                }

                foreach (var discographyGroup in Discography)
                {
                    foreach (var discographyItem in discographyGroup.Items)
                    {
                        if (discographyItem._tracks != null)
                        {
                            foreach (var discographyTrackViewModel in discographyItem._tracks)
                            {
                                var updatedOrNahh =
                                    e.Ids.FirstOrDefault(a => a.Id.Id.Uri == discographyTrackViewModel.Id.Uri);
                                if (updatedOrNahh != null)
                                {
                                    discographyTrackViewModel.IsSaved = updatedOrNahh.Added;
                                }
                            }
                        }
                    }
                }

                RecheckIsSaved();
            }
        }

        private void PlaybackOnPlayingItemChanged(object? sender, ItemId e)
        {
            if ((sender is PlaybackViewModel p))
            {
                if (p.Item?.Context != null && p.Item.Context.Equals(Id))
                {
                    if (_isPlaying != true)
                    {
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            IsPlaying = true;
                        });
                    }
                }
                else
                {
                    if (_isPlaying != false)
                    {
                        IsPlaying = false;
                    }
                }
            }
        }

        public record struct AppleMusicArtist((EumAlbum[] Albums, string Title)? featuredAlbums, string? Header);
        private async Task<AppleMusicArtist?> FetchAppleMusicArtist(string artistName, CancellationToken ct = default)
        {
            await using var stream = await "https://eumhelperapi-p4naoifwjq-dt.a.run.app"
                .AppendPathSegments("AppleSearch", "search", artistName)
                .SetQueryParam("language", "en-GB")
                .SetQueryParam("types", "artists")
                .WithOAuthBearerToken((await Ioc.Default.GetRequiredService<IBearerClient>().GetBearerTokenAsync(ct)))
                .GetStreamAsync(cancellationToken: ct);

            using var jsonDocument = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            //results -> artists -> data[0] -> artwork -> url

            AppleMusicArtist returnData = default;
            using var artists = jsonDocument
                .RootElement.GetProperty("results")
                .GetProperty("artists")
                .GetProperty("data")
                .EnumerateArray();
            if (artists.Any())
            {
                var artist = artists.FirstOrDefault(a => string.Equals(a.GetProperty("attributes")
                        .GetProperty("name").GetString(), artistName,
                    StringComparison.InvariantCultureIgnoreCase));
                if (artist.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    artist = artists.First();

                using var artwork = artist
                    .GetProperty("artwork")
                    .EnumerateArray();
                if (artwork.Any())
                {
                    var first = artwork.FirstOrDefault();
                    if (first.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
                    {
                        //https://is3-ssl.mzstatic.com/image/thumb/Features125/v4/53/0f/ac/530face7-e646-c919-e982-212fb79fbbe0/mzl.lydqqjql.jpg/1478x646vf-60.jpg
                        var url = first.GetProperty("url").GetString()
                            .Replace("{w}", "1478").Replace("{h}bb", "646vf-60")
                            .Replace("{h}ac", "646ac");
                        returnData = new AppleMusicArtist(null, url);
                    }
                }

                var id = artist.GetProperty("id").GetString();

                //https://localhost:44316/artist/320569549
                await using var artistStream = await "https://eumhelperapi-p4naoifwjq-dt.a.run.app"
                    .AppendPathSegments("artist", id)
                    .SetQueryParam("views", "featured-albums")
                    .WithOAuthBearerToken((await Ioc.Default.GetRequiredService<IBearerClient>().GetBearerTokenAsync(ct)))
                    .GetStreamAsync(cancellationToken: ct);
                using var artistDoc = await JsonDocument.ParseAsync(artistStream, cancellationToken: ct);
                //views ->  featured-albums

                var featuredAlbumsView = artistDoc.RootElement.GetProperty("views")
                    .GetProperty("featured-albums");

                using var featuresAlbumsArray = featuredAlbumsView.GetProperty("data")
                    .EnumerateArray();
                if (featuresAlbumsArray.Any())
                {
                    var adaptedData = featuresAlbumsArray
                        .Select(x =>
                        {

                            //Love Yourself 承 'Her'
                            //"LOVE YOURSELF 承 'Her'"
                            var attr = x.GetProperty("attributes");
                            var id = x.GetProperty("id").GetString();
                            var artwork = attr.GetProperty("artwork");
                            var title = attr.GetProperty("name")
                                .GetString();
                            var eumAlbum = new EumAlbum
                            {
                                Name = attr.GetProperty("name")
                                    .GetString()!,
                                Id = Discography.SelectMany(a => a.Items)
                                    .FirstOrDefault(a => string.Equals(title, a.Title, StringComparison.InvariantCultureIgnoreCase))?
                                    .Id ?? new ItemId($"apple:album:{id}"),
                                Images = new[]
                                {
                                    new CachedImage
                                    {
                                        Height = artwork.GetProperty("height")
                                            .GetInt32(),
                                        Width = artwork.GetProperty("width")
                                            .GetInt32(),
                                        Id = artwork.GetProperty("url")
                                            .GetString()!,
                                    }
                                },
                                ArtistsOverride = new IdWithTitle[]
                                {
                                    new IdWithTitle
                                    {
                                        Id = default,
                                        Title = attr.GetProperty("artistName")
                                            .GetString()!
                                    }
                                },
                            };
                            return eumAlbum;
                        }).ToArray();
                    returnData = returnData with
                    {
                        featuredAlbums = (adaptedData, featuredAlbumsView.GetProperty("attributes")
                            .GetProperty("title").GetString()!),

                    };
                }

                return returnData;
            }

            return null;
        }

        public async void OnNavigatedFrom()
        {
            var main = Ioc.Default.GetRequiredService<MainViewModel>();

            var playback = main.PlaybackViewModel;
            playback.PlayingItemChanged -= PlaybackOnPlayingItemChanged;

            var user = main.CurrentUser.User;
            user.LibraryProvider.CollectionUpdated -= LibraryProviderOnCollectionUpdated;

            if (Discography != null)
                foreach (var discographyGroup in Discography)
                {
                    foreach (var discographyGroupItem in discographyGroup.Items)
                    {
                        discographyGroupItem.Cancel();
                        discographyGroupItem.Tracks = null;
                    }

                    discographyGroup.Items = null;
                    discographyGroup.SwitchTemplateCommand = null;
                }

            Discography?.Clear();
            Discography = null;
            foreach (var topTrackViewModel in TopTracks)
            {
                foreach (var cachedImage in topTrackViewModel.Track.Track.Images)
                {
                    var image = await cachedImage.ImageStream;
                    await image.DisposeAsync();
                }
            }
            TopTracks.Clear();
            TopTracks = null;
            GC.Collect();
        }

        public int MaxDepth => 0;

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

        public void RecheckIsSaved()
        {
            var user = Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User;
            IsSaved = user.LibraryProvider.IsSaved(Id);
        }
    }


    [INotifyPropertyChanged]
    public partial class TopTrackViewModel : IIsPlaying, IIsSaved
    {
        [ObservableProperty] private bool _isSaved;
        public TopTrackViewModel(ArtistTopTrack track, ICommand playCommand)
        {
            Track = track;
            PlayCommand = playCommand;
        }
        public ICommand PlayCommand { get; }
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

    public interface IIsSaved
    {

        bool IsSaved { get; set; }
        ItemId Id { get; }
    }

    public class TemplateTypeOrientationWrapper
    {
        public TemplateTypeOrientation Orientation { get; init; }

        public string Glyph => Orientation switch
        {
            TemplateTypeOrientation.Grid => "\uF0E2",
            TemplateTypeOrientation.VerticalStack => "\uE14C"
        };
    }
    [INotifyPropertyChanged]
    public partial class DiscographyGroup
    {
        [ObservableProperty] private TemplateTypeOrientation _templateType;

        private TemplateTypeOrientationWrapper? _selectedItem;

        public TemplateTypeOrientationWrapper[] PossibleViews => new TemplateTypeOrientationWrapper[]
        {
            new() {Orientation = TemplateTypeOrientation.Grid},
            new() {Orientation = TemplateTypeOrientation.VerticalStack}
        };
        public TemplateTypeOrientationWrapper SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value) && value != null)
                {
                    TemplateType = value.Orientation;
                }
            }
        }


        public bool? CanSwitchTemplatesOverride { get; init; }
        public string Title => Type.ToString();
        public DiscographyViewModel[] Items { get; set; }
        public bool CanSwitchTemplates => CanSwitchTemplatesOverride ?? Type is DiscographyType.Album or DiscographyType.Single;
        public DiscographyType Type { get; init; }
        public ICommand SwitchTemplateCommand { get; set; }
        public string Key { get; set; }
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
                _ = Task.Run(FetchIfNeccesary);
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
            var user = Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User;
            Ioc.Default.GetRequiredService<IDispatcherHelper>().TryEnqueue(QueuePriority.Low, () =>
            {
                Tracks = album.Discs.SelectMany(a => a)
                    .Select((z, i) => new DiscographyTrackViewModel
                    {
                        IsLoading = false,
                        Duration = z.Duration,
                        Index = i,
                        Id = new ItemId(z.Uri.Uri),
                        Title = z.Name,
                        Playcount = (long)z.PlayCount,
                        IsSaved = user.LibraryProvider.IsSaved(new ItemId(z.Uri.Uri))
                    }).ToArray();
            });
            //

            //}
        }

        internal DiscographyTrackViewModel[] _tracks;

        public void Cancel()
        {
            try
            {

                _cts?.Dispose();
            }
            catch (Exception)
            {
            }
            _cts = null;
        }
    }

    [INotifyPropertyChanged]
    public partial class DiscographyTrackViewModel : IIsPlaying, IIsSaved
    {
        [ObservableProperty] private bool _isSaved;
        [ObservableProperty] private bool _isLoading;
        public string Title { get; init; }
        public long Playcount { get; init; }
        public int Duration { get; init; }
        public ItemId Id { get; init; }
        public int Index { get; init; }
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