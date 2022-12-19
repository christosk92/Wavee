using Eum.UI.ViewModels.Search;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI.Views.Search
{
    internal class SearchTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TopResult { get; set; }
        public DataTemplate Recommendations { get; set; }
        public DataTemplate Songs { get; set; }
        public DataTemplate SquareImage { get; set; }
        public DataTemplate Artists { get; set; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                TopResultGroup => TopResult,
                RecommendationsResultGroup => Recommendations,
                SongsResultGroup => Songs,
                SquareImageResultGroup => SquareImage,
                ArtistResultGroup => Artists,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}
