namespace Wavee.UI.ViewModel.Wizard;
public interface IWizardViewModel
{
    string Title { get; }
    string? SecondaryActionTitle { get; }
    IObservable<bool> CanGoNext { get; }
    bool CanGoNextVal { get; }
    double Index { get; }
    bool SecondaryActionCanInvokeOverride { get; }
    Task<bool> Submit(int action);
}
