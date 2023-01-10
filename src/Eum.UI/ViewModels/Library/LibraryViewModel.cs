using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Connection;
using Eum.Enums;
using Eum.UI.Services.Library;
using Eum.UI.Users;
using Eum.UI.ViewModels.Sidebar;
using ReactiveUI;

namespace Eum.UI.ViewModels.Library
{
    public sealed partial class LibraryViewModel : SidebarItemViewModel
    {
        [ObservableProperty] private int _libraryCount;
        private EntityType _libarEntityType;
        private readonly EumUser _forUser;
        public LibraryViewModel(EntityType libarEntityType, EumUser forUser)
        {
            _libarEntityType = libarEntityType;
            _forUser = forUser;
        }

        public EntityType LibraryType => _libarEntityType;
        public void RegisterEvents()
        {
            _forUser.LibraryProvider.CollectionUpdated += LibOnCollectionUpdated;
        }

        private async void LibOnCollectionUpdated(object? sender, (EntityType Type, IReadOnlyList<CollectionUpdateNotification> Ids) e)
        {
            if(e.Type != _libarEntityType) return;
            LibraryCount = (await (sender as ILibraryProvider).LibraryCount(_libarEntityType));
        }

        public void UnregisterEvents()
        {
            _forUser.LibraryProvider.CollectionUpdated -= LibOnCollectionUpdated;
        }

        public override string Title
        {
            get
            {
                return _libarEntityType switch
                {
                    EntityType.Album => "Albums",
                    EntityType.Track => "Liked songs",
                    EntityType.Artist => "Artists",
                    EntityType.Show => "Podcasts"
                };
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        public override string Glyph
        {
            get
            {
                return _libarEntityType switch
                {
                    EntityType.Album => "\uE93C",
                    EntityType.Track => "\uEB52",
                    EntityType.Artist => "\uEBDA",
                    EntityType.Show => "\uEB44",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
        
    }
}
