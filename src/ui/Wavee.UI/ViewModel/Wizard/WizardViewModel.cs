using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System.Reactive.Linq;

namespace Wavee.UI.ViewModel.Wizard;
public sealed class WizardViewModel : ObservableObject, IDisposable
{
    private readonly Func<int, IWizardViewModel> _viewModelFactory;
    private IWizardViewModel _currentView;
    private IDisposable? _listener;
    public WizardViewModel(int totalSteps, Func<int, IWizardViewModel> viewModelFactory)
    {
        TotalSteps = totalSteps;
        _viewModelFactory = viewModelFactory;

        GoNextCommand = new AsyncRelayCommand(GoNextAsync, () => CanGoNext);
        GoBackCommand = new RelayCommand(GoBack, () => CanGoBack);
    }

    private void GoBack()
    {
        throw new NotImplementedException();
    }

    private async Task GoNextAsync()
    {
        await CurrentView.Submit();
        if (CurrentView.Index < TotalSteps - 1)
        {
            CurrentView = _viewModelFactory((int)CurrentView.Index + 1);
        }
        else
        {
            IsDone = true;
        }
    }

    public AsyncRelayCommand GoNextCommand { get; }
    public RelayCommand GoBackCommand { get; }
    public int TotalSteps { get; }
    public IWizardViewModel CurrentView
    {
        get => _currentView;
        private set
        {
            if (this.SetProperty(ref _currentView, value))
            {
                _listener?.Dispose();
                _listener = value.CanGoNext
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe((v) =>
                    {
                        GoBackCommand.NotifyCanExecuteChanged();
                        GoNextCommand.NotifyCanExecuteChanged();
                    });

                GoBackCommand.NotifyCanExecuteChanged();
                GoNextCommand.NotifyCanExecuteChanged();
            }
        }
    }
    public bool CanGoNext => CurrentView.Index < TotalSteps - 1 && CurrentView.CanGoNextVal;
    public bool CanGoBack => CurrentView.Index > 0;

    public bool IsDone { get; private set; }
    public void Dispose()
    {
        _listener?.Dispose();
        _listener = null;
    }

    public void Initialize()
    {
        CurrentView = _viewModelFactory(0);
    }
}
