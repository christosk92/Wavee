using ReactiveUI;

namespace Wavee.UI.ViewModels;

public sealed class EnterCredentialsViewModel : ReactiveObject
{
    private string? _errorMessage;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }
}