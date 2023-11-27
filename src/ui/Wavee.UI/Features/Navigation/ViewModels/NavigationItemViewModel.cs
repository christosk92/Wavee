using CommunityToolkit.Mvvm.ComponentModel;

namespace Wavee.UI.Features.Navigation.ViewModels;

public abstract class NavigationItemViewModel : ObservableObject
{
    private NavigationItemViewModel? _selectedItem;
    public virtual NavigationItemViewModel[] Children { get; } = Array.Empty<NavigationItemViewModel>();

    public NavigationItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }
}