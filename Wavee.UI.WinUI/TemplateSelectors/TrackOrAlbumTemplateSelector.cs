using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.AudioItems;

namespace Wavee.UI.WinUI.TemplateSelectors
{
    public class TrackOrAlbumTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Track
        {
            get;
            set;
        }

        public DataTemplate Album
        {
            get;
            set;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                TrackViewModel => Track,
                AlbumViewModel => Album,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}
