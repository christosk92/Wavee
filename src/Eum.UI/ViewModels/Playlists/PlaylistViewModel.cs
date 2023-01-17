using Eum.UI.ViewModels.Sidebar;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.UI.Items;
using Eum.UI.Playlists;
using Eum.UI.ViewModels.Navigation;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Users;
using DynamicData;
using DynamicData.Binding;
using Eum.Logging;
using Eum.UI.Services.Users;
using Eum.Users;
using ReactiveUI;
using ColorThiefDotNet;
using Eum.UI.Helpers;
using Eum.UI.ViewModels.Settings;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Connection;
using Eum.Enums;
using Eum.UI.Services.Library;
using Eum.UI.ViewModels.Artists;
using Eum.UI.ViewModels.Playback;

namespace Eum.UI.ViewModels.Playlists
{
    public abstract partial class PlaylistViewModel : SidebarItemViewModel, IComparable<PlaylistViewModel>, INavigatable, IGlazeablePage, IIsSaved
    {
        public bool NoTracksAndIsNotLoadingComposite => !HasTracks && !IsLoading;


        [NotifyPropertyChangedFor(nameof(NoTracksAndIsNotLoadingComposite))]
        [ObservableProperty]
        private bool _isLoading;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private bool _isSaved;
        private EumUser? _eumUser;
        [ObservableProperty] protected EumPlaylist _playlist;

        [NotifyPropertyChangedFor(nameof(NoTracksAndIsNotLoadingComposite))]
        [ObservableProperty]
        private bool _hasTracks;
        private TimeSpan _totalTrackDuration;

        public PlaylistViewModel(EumPlaylist playlist)
        {
            Playlist = playlist;
            BigHeader = (playlist.Metadata?.ContainsKey("header_image_url_desktop") ?? false)
                ? playlist.Metadata["header_image_url_desktop"]
                : null;
        }

        private void TracksOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Ioc.Default.GetRequiredService<IDispatcherHelper>()
                .TryEnqueue(QueuePriority.High, () =>
                {
                    IsLoading = false;
                    HasTracks = Tracks.Count > 0;
                    TotalTrackDuration = TimeSpan.FromMilliseconds(Tracks.Sum(a => a.Track.Duration));
                });
        }

        public string? BigHeader { get; }
        public void Connect()
        {
            // _tracksListDisposable = _tracksSourceList.Connect()
            //     .Sort(SortExpressionComparer<PlaylistTrackViewModel>
            //         .Ascending(i => i.Index))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Bind(_tracks)
            //     .Subscribe(set =>
            //     {
            //         IsLoading = false;
            //         HasTracks = _tracksSourceList.Count > 0;
            //         TotalTrackDuration = TimeSpan.FromMilliseconds(_tracksSourceList.Items.Sum(a => a.Track.Duration));
            //     });
            Tracks.CollectionChanged += TracksOnCollectionChanged;
            IsLoading = true;
            Task.Run(async () =>
            {
                try
                {
                    await Sync(true);
                }
                catch (Exception x)
                {
                    S_Log.Instance.LogError(x);
                }
            });

            var main = Ioc.Default.GetRequiredService<MainViewModel>();
            main.PlaybackViewModel.PlayingItemChanged += PlaybackOnPlayingItemChanged;
            PlaybackOnPlayingItemChanged(main.PlaybackViewModel, main.PlaybackViewModel.Item?.Id ?? default);
            main.CurrentUser.User.LibraryProvider.CollectionUpdated += LibraryProviderOnCollectionUpdated;
        }
        public RangeObservableCollection<PlaylistTrackViewModel> Tracks { get; } = new();

        private void LibraryProviderOnCollectionUpdated(object? sender, (EntityType Type, IReadOnlyList<CollectionUpdateNotification> Ids) e)
        {
            if (e.Type is EntityType.Playlist or EntityType.Track)
            {
                foreach (var track in Tracks)
                {
                    var updatedOrNahh = e.Ids.FirstOrDefault(a => a.Id.Id.Uri == track.Id.Uri);
                    if (updatedOrNahh != null)
                    {
                        track.IsSaved = updatedOrNahh.Added;
                    }
                }
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



        public void Disconnect()
        {
            Tracks.CollectionChanged -= TracksOnCollectionChanged;
            var main = Ioc.Default.GetRequiredService<MainViewModel>();
            main.PlaybackViewModel.PlayingItemChanged -= PlaybackOnPlayingItemChanged;
            main.CurrentUser.User.LibraryProvider.CollectionUpdated -= LibraryProviderOnCollectionUpdated;
            Tracks.Clear();
        }
        public TimeSpan TotalTrackDuration
        {
            get => _totalTrackDuration;
            set => this.SetProperty(ref _totalTrackDuration, value);
        }

        public EumUser EumUser => _eumUser ??= Ioc.Default.GetRequiredService<IEumUserManager>()
            .GetUser(_playlist.User);

        public override string Title
        {
            get => Playlist.Name;
            protected set => throw new NotSupportedException();
        }
        public override string Glyph => "\uE93F";
        public override string GlyphFontFamily => "/Assets/MediaPlayerIcons.ttf#Media Player Fluent Icons";
        public int CompareTo(PlaylistViewModel? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Playlist.Order.CompareTo(other.Playlist.Order);
        }
        public bool IsActive { get; set; }
        public ICommand SortCommand { get; }

        public static PlaylistViewModel Create(EumPlaylist user)
        {
            switch (user.Id.Service)
            {
                case ServiceType.Local:
                    break;
                case ServiceType.Spotify:
                    return new SpotifyPlaylistViewModel(user);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return default;
        }

        public abstract Task Sync(bool addTracks = false);
        public void OnNavigatedTo(object parameter)
        {
            Connect();
        }

        public void OnNavigatedFrom()
        {
            Disconnect();
        }

        public int MaxDepth => 2;

        public bool ShouldSetPageGlaze => Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.ThemeService
            .Glaze == "Page Dependent";

        public async ValueTask<string> GetGlazeColor(AppTheme theme, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(Playlist.ImagePath))
            {
                try
                {
                    var colorsClient = Ioc.Default.GetRequiredService<IExtractedColorsClient>();
                    var uri = new Uri(Playlist.ImagePath);
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
                        .GetStreamForString(Playlist.ImagePath, ct);
                    using var bmp = new Bitmap(fs);
                    var colorThief = new ColorThief();
                    var c = colorThief.GetPalette(bmp);

                    return c[0].Color.ToHexString();
                }
            }

            return string.Empty;
        }

        public ItemId Id => _playlist.Id;
    }
}
