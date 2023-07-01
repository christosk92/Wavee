using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.User;

namespace Wavee.UI.ViewModel.Shell;

public sealed class RightSidebarViewModel : ObservableObject
{
    private readonly UserViewModel _user;
    private bool _show;
    private RightSidebarView _view;

    public RightSidebarViewModel(UserViewModel user)
    {
        _user = user;
    }

    public bool Show
    {
        get => _show;
        set
        {
            if (SetProperty(ref _show, value))
            {
                this.OnPropertyChanged(nameof(IsInLyricsView));
            }
        }
    }

    public RightSidebarView View
    {
        get => _view;
        set
        {
            if (SetProperty(ref _view, value))
            {
                this.OnPropertyChanged(nameof(IsInLyricsView));
            }
        }
    }

    public bool IsInLyricsView => View == RightSidebarView.Lyrics && Show;

    public void ShowView(RightSidebarView view)
    {
        View = view;
        Show = true;
    }

    public void Hide()
    {
        Show = false;
        View = RightSidebarView.Lyrics;
    }
}
public enum RightSidebarView
{
    Lyrics
}