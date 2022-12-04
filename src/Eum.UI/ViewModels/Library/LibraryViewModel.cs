using Eum.Enums;
using Eum.UI.ViewModels.Sidebar;

namespace Eum.UI.ViewModels.Library
{
    public sealed class LibraryViewModel : SidebarItemViewModel
    {
        private EntityType _libarEntityType;
        public LibraryViewModel(EntityType libarEntityType)
        {
            _libarEntityType = libarEntityType;
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
