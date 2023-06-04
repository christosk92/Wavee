using System;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace Wavee.UI.WinUI.Views.Shell;

public class SidebarItem : ReactiveObject
{
    private int _count;
    public bool IsEnabled { get; init; } = true;
    public bool IsAHeader { get; init; }
    public string Title { get; init; }
    public IconElement? Icon { get; init; }
    public Action? Navigation { get; init; }
    public Func<object, bool>? DidNavigateTo { get; init; }

    public bool IsCountable { get; init; }

    public int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }
}