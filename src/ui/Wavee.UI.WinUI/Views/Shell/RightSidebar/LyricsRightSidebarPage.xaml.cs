using System;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Features.Shell.ViewModels;
using Wavee.UI.WinUI.Contracts;

namespace Wavee.UI.WinUI.Views.Shell.RightSidebar;

public sealed partial class LyricsRightSidebarPage : Page, INavigeablePage<RightSidebarLyricsViewModel>
{
    public LyricsRightSidebarPage()
    {
        this.InitializeComponent();
    }

    public void UpdateBindings()
    {
       // this.Bindings.Update();
    }

    public RightSidebarLyricsViewModel ViewModel
    {
        get
        {
            try
            {
                return DataContext is RightSidebarLyricsViewModel vm ? vm : null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }
}