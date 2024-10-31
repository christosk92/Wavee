using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models.EventArgs;

namespace ConsoleApp1.PseudoUI;

public class ActivatableApplicationLifetime : IActivatableApplicationLifetime
{
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
        // on console, there is no background
    }
}