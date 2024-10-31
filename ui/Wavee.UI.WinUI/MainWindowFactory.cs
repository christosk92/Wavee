using System.Reactive.Concurrency;
using ReactiveUI;
using Wavee.ViewModels.Interfaces;

namespace Wavee.UI.WinUI;

public sealed class MainWindowFactory : IMainWindowFactory
{
    public IMainWindow Create()
    {
        var window = new MainWindow();
        RxApp.MainThreadScheduler = new DispatcherQueueScheduler(window.DispatcherQueue);
        return window;
    }
}