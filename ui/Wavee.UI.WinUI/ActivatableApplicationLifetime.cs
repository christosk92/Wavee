using System;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models.EventArgs;

namespace Wavee.UI.WinUI;

public sealed class ActivatableApplicationLifetime : IActivatableApplicationLifetime
{
    private readonly App _app;
    public ActivatableApplicationLifetime(App app)
    {
        _app = app;
    }

    public void TryEnterBackground()
    {
        throw new NotImplementedException();
    }

    public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;
    public event EventHandler<ActivatedEventArgs>? Activated;
    public event EventHandler<ActivatedEventArgs>? Deactivated;
    public void Shutdown()
    {
        throw new NotImplementedException();
    }

    public IMainWindow MainWindow { get; set; }
    public void TryLeaveBackground()
    {

    }
}