using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Library.ViewModels.Album;
using Wavee.UI.WinUI.Contracts;
using Wavee.UI.Features.Search.ViewModels;


namespace Wavee.UI.WinUI.Views.Search;

public sealed partial class SearchPage : Page, INavigeablePage<SearchViewModel>
{
    public SearchPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SearchViewModel vm)
        {
            DataContext = vm;
        }
    }

    public void UpdateBindings()
    {
        //this.Bindings.Update();
    }

    public SearchViewModel ViewModel => DataContext is SearchViewModel vm ? vm : null;

    private void FrameworkElement_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        
    }
}