using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.UI.Spotify.ViewModels.Users;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI.Views.Login
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
