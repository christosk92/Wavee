using Eum.UI.ViewModels.Sidebar;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace Eum.UI.ViewModels.Playlists
{
    public abstract partial class PlaylistViewModel : SidebarItemViewModel, IComparable<PlaylistViewModel>, INavigatable
    {
        private EumUser? _eumUser;
        [ObservableProperty] protected EumPlaylist _playlist;
        private IDisposable _tracksListDisposable;
        protected readonly SourceList<PlaylistTrackViewModel> _tracksSourceList = new();
        private readonly ObservableCollectionExtended<PlaylistTrackViewModel> _tracks = new();
        private bool _hasTracks;
        private TimeSpan _totalTrackDuration;

        public PlaylistViewModel(EumPlaylist playlist)
        {
            Playlist = playlist;
        }

        public void Connect()
        {
            _tracksListDisposable = _tracksSourceList.Connect()
                .Sort(SortExpressionComparer<PlaylistTrackViewModel>
                    .Ascending(i => i.Index))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(_tracks)
                .Subscribe(set =>
                {
                    HasTracks = _tracksSourceList.Count > 0;
                    TotalTrackDuration = TimeSpan.FromMilliseconds(_tracksSourceList.Items.Sum(a => a.Track.Duration));
                });

            Task.Run(async () =>
            {
                try
                {
                    await Sync();
                }
                catch (Exception x)
                {
                    S_Log.Instance.LogError(x);
                }
            });
        }

        public void Disconnect()
        {
            _tracksListDisposable?.Dispose();
            _tracksSourceList.Clear();
            _tracks.Clear();
        }
        public TimeSpan TotalTrackDuration
        {
            get => _totalTrackDuration;
            set => this.SetProperty(ref _totalTrackDuration, value);
        }

        public ObservableCollectionExtended<PlaylistTrackViewModel> Tracks => _tracks;

        public EumUser EumUser => _eumUser ??= Ioc.Default.GetRequiredService<IEumUserManager>()
            .GetUser(_playlist.User);
        public bool HasTracks
        {
            get => _hasTracks;
            set => this.SetProperty(ref _hasTracks, value);
        }
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
        public void OnNavigatedTo(bool isInHistory)
        {
            Connect();
        }

        public void OnNavigatedFrom(bool isInHistory)
        {
            Disconnect();
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

        public abstract Task Sync();
    }
}
