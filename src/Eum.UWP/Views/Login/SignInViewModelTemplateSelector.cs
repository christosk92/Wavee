using Eum.UI.Spotify.ViewModels.Users;
using Eum.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Eum.UWP.Views.Login
{

    class SignInViewModelTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Null { get; set; }
        public DataTemplate Spotify { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                SignInToSpotifyViewModel => Spotify,
                EmptyViewModel => Null,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}
