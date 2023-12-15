using Microsoft.UI.Xaml.Controls;
using System;
using Wavee.UI.Features.RightSidebar.ViewModels;
using Wavee.UI.WinUI.Contracts;

namespace Wavee.UI.WinUI.Views.Shell.RightSidebar;

public sealed partial class QueueRightSidebarPage : Page, INavigeablePage<RightSidebarQueueViewModel>
{
    public QueueRightSidebarPage()
    {
        this.InitializeComponent();
    }

    public void UpdateBindings()
    {
        // this.Bindings.Update();
    }

    public RightSidebarQueueViewModel ViewModel
    {
        get
        {
            try
            {
                return DataContext is RightSidebarQueueViewModel vm ? vm : null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }
}