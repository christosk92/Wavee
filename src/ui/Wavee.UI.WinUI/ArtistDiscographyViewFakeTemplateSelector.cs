using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.WinUI.Views.Artist.Sections;

namespace Wavee.UI.WinUI;

public sealed class ArtistDiscographyViewFakeTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            ArtistDiscographyViewFake fake =>
                fake.IsList ? ListTemplate : GridTemplate,
            _ => base.SelectTemplateCore(item, container)
        };
    }

    public DataTemplate GridTemplate { get; set; }

    public DataTemplate ListTemplate { get; set; }
}