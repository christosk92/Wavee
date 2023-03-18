using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Login.Impl;

namespace Wavee.UI.WinUI.TemplateSelectors;

public sealed class LoginServiceTemplateSelector : DataTemplateSelector
{
    public DataTemplate ListProfiles { get; set; }
    public DataTemplate Spotify { get; set; }
    public DataTemplate Local { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            SpotifyLoginViewModel => Spotify,
            CreateLocalProfileViewModel => Local,
            SelectProfileViewModel => ListProfiles,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}