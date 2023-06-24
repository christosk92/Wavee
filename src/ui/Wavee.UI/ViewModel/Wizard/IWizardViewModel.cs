namespace Wavee.UI.ViewModel.Wizard;
public interface IWizardViewModel
{
    string Title { get; }
    IObservable<bool> CanGoNext { get; }
    bool CanGoNextVal { get; }
    double Index { get; }

    Task Submit();
}
