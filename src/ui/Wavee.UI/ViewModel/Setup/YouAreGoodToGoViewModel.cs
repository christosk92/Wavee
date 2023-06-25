using System.Reactive.Linq;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Wizard;

namespace Wavee.UI.ViewModel.Setup;

public sealed class YouAreGoodToGoViewModel : IWizardViewModel
{
    public IObservable<bool> CanGoNext => Observable.Return(true);
    public bool CanGoNextVal => true;
    public double Index => 4;
    public Task<bool> Submit(int action)
    {
        return Task.FromResult(true);
    }

    public string Title { get; }
    public string? SecondaryActionTitle { get; }
    public bool SecondaryActionCanInvokeOverride { get; }
    public static UserViewModel User { get; set; }
}