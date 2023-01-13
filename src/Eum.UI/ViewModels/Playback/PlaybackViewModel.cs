using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Eum.Artwork;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Playback.Player;
using Eum.Enums;
using Eum.Spotify.connectstate;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.Services.Library;
using Eum.UI.Users;
using Eum.UI.ViewModels.Artists;
using ReactiveUI;

namespace Eum.UI.ViewModels.Playback
{
    [INotifyPropertyChanged]
    public abstract partial class PlaybackViewModel : IIsSaved
    {
        [ObservableProperty]
        private CurrentlyPlayingHolder _item;

        [ObservableProperty] private bool _isSaved;

        [ObservableProperty]
        private bool _playingOnExternalDevice;

        [ObservableProperty]
        private RemoteDevice _externalDevice;

        [ObservableProperty] private bool _isPaused;

        [ObservableProperty] private double _timestamp;
        [ObservableProperty] private bool _isShuffling;
        [ObservableProperty] private double _volume;
        [ObservableProperty] private bool _canChangeVolume;
        [NotifyPropertyChangedFor(nameof(IsRepeatingAny))]
        [ObservableProperty]
        private RepeatMode _repeatMode;
        private IDisposable _disposable;
        private IDisposable _secondDisposable;

        private ItemId _activeDeviceId;

        public bool IsRepeatingAny => _repeatMode != RepeatMode.None;

        // protected PlaybackViewModel()
        // {
        //     Ioc.Default.GetRequiredService<MainViewModel>()
        //         .CurrentUser.User.LibraryProvider.CollectionUpdated += LibraryProviderOnCollectionUpdated;
        // }

        private void LibraryProviderOnCollectionUpdated(object? sender, (EntityType Type, IReadOnlyList<CollectionUpdateNotification> Ids) e)
        {
            if (e.Type == EntityType.Track || e.Type == EntityType.Episode)
            {
                var interestedIn = e.Ids.FirstOrDefault(a => a.Id.Id.Uri == Item.Id.Uri);
                if (interestedIn != null)
                {
                    IsSaved = interestedIn.Added;
                }
            }
        }

        public ICommand NavigateToAlbum => Commands.To(EntityType.Album);
        public ObservableCollection<RemoteDevice> RemoteDevices { get; } = new ObservableCollection<RemoteDevice>();
        public abstract ServiceType Service { get; }

        public virtual void Construct(EumUser user)
        {
            user.LibraryProvider.CollectionUpdated += LibraryProviderOnCollectionUpdated;
        }
        public virtual void Deconstruct(EumUser user)
        {
            StopTimer();
            user.LibraryProvider.CollectionUpdated -= LibraryProviderOnCollectionUpdated;
        }

        protected void StopTimer()
        {
            _disposable?.Dispose();
        }

        protected void StartTimer(long atPosition)
        {
            StopTimer();
            Timestamp = atPosition;

            _disposable =  Observable.Interval(TimeSpan.FromMilliseconds(200), RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async l =>
                {
                    if (PlayingOnExternalDevice)
                    {
                        Timestamp += 200;
                    }
                    else
                    {
                        Timestamp = await Ioc.Default.GetRequiredService<IAudioPlayer>()
                            .Time(_item.PlaybackId);
                    }
                });
        }

        public event EventHandler<ItemId> PlayingItemChanged;
        public abstract Task SwitchRemoteDevice(ItemId? deviceId);

        public ItemId ActiveDeviceId
        {
            get => _activeDeviceId;
            protected set
            {
                if (_activeDeviceId != value)
                {
                    OnPropertyChanged(nameof(ActiveDeviceId));
                    _activeDeviceId = value;
                    ActiveDeviceChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<double> Seeked;
        public event EventHandler<bool> Paused;
        public event EventHandler<ItemId> ActiveDeviceChanged;
        protected virtual async void OnPlayingItemChanged(ItemId e)
        {
            PlayingItemChanged?.Invoke(this, e);
            IsSaved = await Task.Run(() => Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.LibraryProvider
                .IsSaved(e));
        }

        protected virtual void OnSeeked(double e)
        {
            Seeked?.Invoke(this, e);
        }

        public ItemId Id => Item?.Id ?? default;
    }

    public record RemoteDevice(ItemId DeviceId, string DeviceName, DeviceType Devicetype);


    [INotifyPropertyChanged]
    public partial class CurrentlyPlayingHolder : IDisposable
    {
        [ObservableProperty] private string? _playbackId;
        public Stream BigImage { get; init; }
        public Stream SmallImage { get; init; }

        public Uri BigImageUrl { get; init; }
        public ItemId Context { get; init; }
        public IdWithTitle Title { get; init; }
        public IdWithTitle[] Artists { get; init; }
        public double Duration { get; init; }
        public ItemId Id { get; init; }

        public void Dispose()
        {
            BigImage.Dispose();
            SmallImage.Dispose();
        }
    }

    public class IdWithTitle
    {
        public ItemId Id { get; init; }
        public string Title { get; init; }
    }
}
