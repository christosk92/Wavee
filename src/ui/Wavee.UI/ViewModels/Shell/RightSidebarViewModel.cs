using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.NowPlaying;

namespace Wavee.UI.ViewModels.Shell;

public sealed class RightSidebarViewModel : ObservableObject
{
    private readonly IDispatcher _dispatcher;
    private RightSidebarComponentViewModel? _currentSidebarItem;

    public RightSidebarViewModel(
        LyricsViewModel lyrics,
        IDispatcher dispatcher)
    {
        Lyrics = lyrics;
        _dispatcher = dispatcher;
        Components = [lyrics];
        CurrentSidebarItem = NullSidebarComponentViewModel.Instance;
        OpenSidebarComponentCommand = new AsyncRelayCommand<object>(async x =>
        {
            if (x is string typeStr)
            {
                var type = Type.GetType(typeStr);
                var component = Components.First(x => x.GetType() == type);
                if (_currentSidebarItem is not null)
                {
                    await _currentSidebarItem.Closed();
                }

                if (_currentSidebarItem == component)
                {
                    CurrentSidebarItem = NullSidebarComponentViewModel.Instance;
                }
                else
                {
                    CurrentSidebarItem = component;
                    await component.Opened();
                }
            }
            else if (x is RightSidebarComponentViewModel y)
            {
                if (CurrentSidebarItem != y)
                {
                    CurrentSidebarItem = y;
                    await y.Opened();
                }
            }
        });
    }
    public IReadOnlyCollection<RightSidebarComponentViewModel> Components { get; }

    public RightSidebarComponentViewModel? CurrentSidebarItem
    {
        get => _currentSidebarItem;
        set
        {
            if (this.SetProperty(ref _currentSidebarItem, value))
            {
                this.OnPropertyChanged(nameof(ShouldBeOpen));
            }
        }
    }

    public bool ShouldBeOpen => CurrentSidebarItem is not NullSidebarComponentViewModel;

    public AsyncRelayCommand<object> OpenSidebarComponentCommand { get; }
    public LyricsViewModel Lyrics { get; }
}

public abstract class RightSidebarComponentViewModel : ObservableObject
{
    public abstract ValueTask Opened();
    public abstract ValueTask Closed();
}

public sealed class NullSidebarComponentViewModel : RightSidebarComponentViewModel
{
    public static NullSidebarComponentViewModel Instance { get; } = new NullSidebarComponentViewModel();
    public override ValueTask Opened()
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask Closed()
    {
        return ValueTask.CompletedTask;
    }
}