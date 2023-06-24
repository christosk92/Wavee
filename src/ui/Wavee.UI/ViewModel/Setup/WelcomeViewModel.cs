using System.Reactive.Linq;
using Wavee.UI.ViewModel.Wizard;

namespace Wavee.UI.ViewModel.Setup;
public sealed class WelcomeViewModel : IWizardViewModel
{
    public string Title => "Hey!";

    public IObservable<bool> CanGoNext => Observable.Return(true);

    public bool CanGoNextVal => true;

    public double Index => 0;

    public Task Submit()
    {
        throw new NotImplementedException();
    }
}
