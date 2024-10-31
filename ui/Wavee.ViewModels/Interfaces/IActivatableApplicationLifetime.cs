using Wavee.ViewModels.Models.EventArgs;

namespace Wavee.ViewModels.Interfaces;

public interface IActivatableApplicationLifetime
{
    void TryEnterBackground();
    event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;
    event EventHandler<ActivatedEventArgs>? Activated;
    event EventHandler<ActivatedEventArgs>? Deactivated;
    void Shutdown();
    IMainWindow MainWindow { get; set; }
    void TryLeaveBackground();
}