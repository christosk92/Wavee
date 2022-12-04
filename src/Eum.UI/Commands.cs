using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Items;
using Eum.UI.ViewModels.Artists;
using Eum.UI.ViewModels.Navigation;

namespace Eum.UI
{
    public static class Commands
    {
        static Commands()
        {
            ToArtist = new RelayCommand<ItemId>(id =>
            {
                var artistViewmodel = new ArtistRootViewModel
                {
                    Id = id
                };

                NavigationState.Instance.HomeScreenNavigation.To(artistViewmodel);
            });
        }
        public static ICommand ToArtist { get; }

        public static ICommand To(EumEntityType argId)
        {
            switch (argId)
            {
                case EumEntityType.Artist:
                    return ToArtist;
            }

            return default;
        }
    }
}
