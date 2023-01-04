using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Search;

namespace Eum.UWP.Views.Search
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
