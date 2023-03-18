using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Libray;

namespace Wavee.UI.WinUI.TemplateSelectors
{
    internal sealed class LibaryViewSelector : DataTemplateSelector
    {
        public DataTemplate Songs { get; set; }
        public DataTemplate Albums { get; set; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                LibrarySongsViewModel => Songs,
                LibraryAlbumsViewModel => Albums,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}
