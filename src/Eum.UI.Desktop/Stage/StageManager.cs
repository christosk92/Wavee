using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.ViewModels.Login;

namespace Eum.UI.Stage;

[INotifyPropertyChanged]
public partial class StageManager
{
    [ObservableProperty]
    private IStage _currentStage;

    [ObservableProperty]
    private bool _canGoBack;
    private readonly Stack<IStage> _stageStack = new();
    public StageManager(IStage initialStage, int numberOfStages)
    {
        _currentStage = initialStage;
        NumberOfStages = numberOfStages;
        GoBackCommand = new RelayCommand(GoBack);
        GoNextCommand = new RelayCommand(GoNext);
    }
    public ICommand GoBackCommand { get; }
    public ICommand GoNextCommand { get; }
    public void GoBack()
    {
        if (_stageStack.Count == 0) return;
        if (CurrentStage is IDisposable disposable)
        {
            disposable.Dispose();
        }
        CurrentStage = _stageStack.Pop();

        CanGoBack = _stageStack.Count > 0;
        return;
    }

    public void GoNext()
    {
        var (nextStage, result) = _currentStage.NextStage();
        if (nextStage != null)
        {
            _stageStack.Push(_currentStage);
            CurrentStage = nextStage;
            CanGoBack = _stageStack.Count > 0;
            return;
        }
        else
        {
            FinalResult = result;
            CompletedCallback(result);

            foreach (var stage in _stageStack)
            {
                if (stage is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            if (CurrentStage is IDisposable d)
            {
                d.Dispose();
            }
            return;
        }
    }
    public object? FinalResult { get; private set; }
    public WizardType WizardType { get; set; }
    public int NumberOfStages { get; }

    public void RegisterCompletedCallback(Action<object> action)
    {
        CompletedCallback = action;
    }

    private Action<object> CompletedCallback;
}

public enum WizardType
{
    Pips
}