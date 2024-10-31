namespace Wavee.ViewModels.Models.EventArgs;

public sealed class ShutdownRequestedEventArgs : System.EventArgs
{
    public ShutdownRequestedEventArgs(bool cancel)
    {
        Cancel = cancel;
    }

    public bool Cancel { get; set; }
}