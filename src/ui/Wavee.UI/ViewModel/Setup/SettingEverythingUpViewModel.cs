using System.Reactive.Linq;
using System.Reactive.Subjects;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.Id;
using Wavee.UI.Contracts;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Wizard;

namespace Wavee.UI.ViewModel.Setup;

public sealed class SettingEverythingUpViewModel : ObservableObject, IWizardViewModel, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Func<ServiceType, IMusicEnvironment> _environmentFactory;
    private readonly Subject<bool> _canGoNext = new();
    private readonly IDisposable _listener;
    private bool _canGoNextVal;
    private double _progressPercentage;

    public SettingEverythingUpViewModel(Func<ServiceType, IMusicEnvironment> environmentFactory)
    {
        _environmentFactory = environmentFactory;
        _listener = _canGoNext.Subscribe(x =>
        {
            CanGoNextVal = x;
        });
    }

    public string Title => "Setting everything up";
    public IObservable<bool> CanGoNext => _canGoNext.AsObservable();

    public bool CanGoNextVal
    {
        get => _canGoNextVal;
        set => SetProperty(ref _canGoNextVal, value);
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => SetProperty(ref _progressPercentage, value);
    }

    public double Index => 2;

    public async Task<bool> Submit(int action)
    {
        if (action ==1 )
        {
            await PerformMiracle(User);
            return false;
        }

        return true;
    }

    private async Task PerformMiracle(UserViewModel user)
    {
        try
        {
            //dummy increase progress every 100ms
            while (ProgressPercentage < 100)
            {
                ProgressPercentage += 0.5;
                await Task.Delay(100, _cts.Token);
            }
        }
        catch (TaskCanceledException)
        {
            //ignore
        }
    }

    public bool SecondaryActionCanInvokeOverride { get; }
    public string? SecondaryActionTitle { get; }
    public static UserViewModel User { get; set; }

    public void Dispose()
    {
        _canGoNext.Dispose();
        _listener.Dispose();
        _cts.Cancel();
        User?.Dispose();
    }
}