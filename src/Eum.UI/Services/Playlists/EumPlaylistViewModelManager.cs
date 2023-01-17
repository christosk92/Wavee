using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using DynamicData;
using Eum.UI.Playlists;
using Eum.UI.Services.Users;
using Eum.UI.Users;
using Eum.UI.ViewModels.Playlists;
using Nito.AsyncEx;
using ReactiveUI;
using System.Diagnostics.CodeAnalysis;
using Eum.Users;
using System.Collections.ObjectModel;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Spotify.metadata;
using Eum.UI.Items;
using Medialoc.Shared.Helpers;

namespace Eum.UI.Services.Playlists
{
    [INotifyPropertyChanged]
    public sealed partial class EumPlaylistViewModelManager : IEumUserPlaylistViewModelManager
    {
        private readonly SourceList<PlaylistViewModel> _playlistsSourceList = new();
        private readonly ObservableCollectionExtended<PlaylistViewModel> _playlists = new();

        private readonly IEumPlaylistManager _playlistManager;

        private List<ItemId> _ignoreSet = new List<ItemId>();
        public EumPlaylistViewModelManager(IEumPlaylistManager playlistManager,
            IEumUserViewModelManager userViewModelManager)
        {
            _playlistManager = playlistManager;
            _playlistsSourceList
                .Connect()
                .Sort(SortExpressionComparer<PlaylistViewModel>
                    .Ascending(i => i.Playlist.Order))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(_playlists)
                .Subscribe();

            Observable
                .FromEventPattern<EumPlaylist>(_playlistManager, nameof(IEumPlaylistManager.PlaylistAdded))
                .Select(x => x.EventArgs)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(user =>
                {
                    if (_ignoreSet.Contains(user.Id)) return;
                    var vm = PlaylistViewModel.Create(user);
                    _playlistsSourceList.Add(vm);
                });

            Observable
                .FromEventPattern<EumPlaylist>(_playlistManager, nameof(IEumPlaylistManager.PlaylistUpdated))
                .Select(x => x.EventArgs)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(user =>
                {
                    if (_ignoreSet.Contains(user.Id)) return;
                    if (TryGetPlaylistViewModel(user, out var toUpdate))
                    {
                        toUpdate.Playlist = user;
                    }
                });

            Observable
                .FromEventPattern<EumPlaylist>(_playlistManager, nameof(IEumPlaylistManager.PlaylistRemoved))
                .Select(x => x.EventArgs)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(async wallet =>
                {
                    _ignoreSet.Add(wallet.Id);
                    await Task.Run(async () => await WaitForVm(wallet));
                    if (TryGetPlaylistViewModel(wallet, out var toRemoveUser))
                    {
                        _playlistsSourceList.Remove(toRemoveUser);

                        var safeIo = new SafeIoManager(wallet.FilePath);
                        safeIo.DeleteMe();
                    }
                })
                .Subscribe();


            Observable
                .FromEventPattern<EumUserViewModel>(userViewModelManager, nameof(IEumUserViewModelManager.CurrentUserChanged))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x.EventArgs as EumUserViewModel)
                .Subscribe(user =>
                {
                    CurrentUser = user;

                    SetupNewListeners(user);
                });

            CurrentUser = userViewModelManager.CurrentUser;
            if (CurrentUser != null)
            {
                SetupNewListeners(CurrentUser);
            }
            AsyncContext.Run(async () =>
            {
                await EnumeratePlaylists();
            });
        }

        private void DisconnectListeners()
        {

        }
        private void SetupNewListeners(EumUserViewModel user)
        {
            //listen to changes on the external service.
            DisconnectListeners();


        }

        public SourceList<PlaylistViewModel> SourceList => _playlistsSourceList;
        public EumUserViewModel CurrentUser { get; private set; }
        public async Task<PlaylistViewModel> WaitForVm(EumPlaylist playlist)
        {
            PlaylistViewModel playlistViewModel;
            while (!TryGetPlaylistViewModel(playlist, out playlistViewModel))
            {
                await Task.Delay(10);
            }

            return playlistViewModel;
        }
        public ObservableCollection<PlaylistViewModel> Playlists => _playlists;

        private bool TryGetPlaylistViewModel(EumPlaylist playlist, out PlaylistViewModel? playlistViewModel)
        {
            playlistViewModel = Playlists.FirstOrDefault(x => x.Playlist.Id == playlist.Id);
            return playlistViewModel is { };
        }


        private async Task EnumeratePlaylists()
        {
            foreach (var user in await _playlistManager.GetPlaylists(CurrentUser.User.Id, true))
            {
                var playlist = PlaylistViewModel.Create(user);
                _playlistsSourceList.Add(playlist);
            }
        }
    }
}
