namespace Wavee.UI.ViewModels.Shell;

internal sealed class ShellViewModelFactory : IShellViewModelFactory
{
    public IShellViewModel Create(ProfileContext profileContext)
    {
        throw new NotImplementedException();
    }
}

public interface IShellViewModelFactory
{
    IShellViewModel Create(ProfileContext profileContext);
}