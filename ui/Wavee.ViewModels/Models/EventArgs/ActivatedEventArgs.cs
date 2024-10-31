namespace Wavee.ViewModels.Models.EventArgs;

public sealed class ActivatedEventArgs : System.EventArgs
{
    public ActivatedEventArgs(bool activated, ActivationKind kind)
    {
        Activated = activated;
        Kind = kind;
    }

    public bool Activated { get; }
    public ActivationKind Kind { get; }
}

public enum ActivationKind
{
    Background,
    Reopen
}