using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.AudioItems;

namespace Wavee.UI.WinUI.TemplateSelectors
{
    public class TrackOrAlbumStyleSelector : StyleSelector
    {
        public Style Track
        {
            get;
            set;
        }

        public Style Album
        {
            get;
            set;
        }

        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            return item switch
            {
                TrackViewModel => Track,
                AlbumViewModel => Album,
                _ => base.SelectStyleCore(item, container)
            };
        }
    }
}