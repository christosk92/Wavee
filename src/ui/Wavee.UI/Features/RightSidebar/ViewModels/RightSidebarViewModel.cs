using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Features.Navigation;

namespace Wavee.UI.Features.RightSidebar.ViewModels;

public sealed class RightSidebarViewModel : ObservableObject
{
    private bool _isOpen;
    private bool _isDocked = true;
    private RightSidebarItemViewModel? _selectedItem;
    private double _sidebarWidth = 200;

    public RightSidebarViewModel(
        RightSidebarLyricsViewModel lyrics,
        RightSidebarQueueViewModel queue)
    {
        Items = new List<RightSidebarItemViewModel>()
        {
            lyrics,
            queue
        };
    }

    public bool IsOpen
    {
        get => _isOpen;
        set => SetProperty(ref _isOpen, value);
    }

    public double SidebarWidth
    {
        get => _sidebarWidth;
        set => SetProperty(ref _sidebarWidth, value);
    }

    public bool IsDocked
    {
        get => _isDocked;
        set => SetProperty(ref _isDocked, value);
    }

    public RightSidebarItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value is null)
            {
                IsOpen = false;
            }

            SetProperty(ref _selectedItem, value);
            IsOpen = value is not null;
        }
    }

    public List<RightSidebarItemViewModel> Items { get; }
    public INavigationService Navigation { get; set; }
}