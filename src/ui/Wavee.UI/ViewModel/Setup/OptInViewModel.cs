using System.Reactive.Linq;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Wizard;

namespace Wavee.UI.ViewModel.Setup;

public sealed class OptInViewModel : IWizardViewModel
{
    public IObservable<bool> CanGoNext => Observable.Return(true);
    public bool CanGoNextVal => true;
    public double Index => 3;
    public Task<bool> Submit(int action)
    {
        YouAreGoodToGoViewModel.User = User;
        return Task.FromResult(true);
    }

    public string Title { get; }
    public string? SecondaryActionTitle { get; }
    public bool SecondaryActionCanInvokeOverride { get; }
    public static UserViewModel User { get; set; }
    public UserSettings Settings => User.Settings;
}