using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.Domain.Playback;
using Wavee.Spotify.Domain.Remote;
using Wavee.UI.Features.Dialog.Queries;
using Wavee.UI.Features.Playback.ViewModels;

namespace Wavee.UI.Features.Dialog.ViewModels;

public sealed class NoActiveDeviceSelectionDialogViewModel : ObservableObject
{
    private readonly PlaybackViewModel _playbackViewModel;
    private RemoteDevice _selectedDevice;
    private bool? _alwaysDoThis;
    private PromptDeviceSelectionResultType _whichOne;
    private TaskCompletionSource<PromptDeviceSelectionResult> _result = new();
    private bool _continueCommandCanExecute;

    public NoActiveDeviceSelectionDialogViewModel(PlaybackViewModel playbackViewModel)
    {
        _playbackViewModel = playbackViewModel;

        ContinueCommand = new RelayCommand(() =>
        {
            _result.SetResult(new PromptDeviceSelectionResult(WhichOne, SelectedDevice?.Id, _alwaysDoThis ?? false));
        }, canExecute: CanContinue);

        CancelCommand = new RelayCommand(() =>
        {
            WhichOne = PromptDeviceSelectionResultType.Nothing;
            SelectedDevice = null;

            _result.SetResult(new PromptDeviceSelectionResult(PromptDeviceSelectionResultType.Nothing, null, false));
        });
    }

    public Task<PromptDeviceSelectionResult> Result => _result.Task;
    public RemoteDevice OwnDevice => _playbackViewModel.OwnDevice;
    public ObservableCollection<RemoteDevice> AvailableDevices => _playbackViewModel.Devices;
    public RelayCommand ContinueCommand { get; }
    public RelayCommand CancelCommand { get; }

    public PromptDeviceSelectionResultType WhichOne
    {
        get => _whichOne;
        set
        {
            if (SetProperty(ref _whichOne, value))
            {
                ContinueCommand.NotifyCanExecuteChanged();

                ContinueCommandCanExecute = CanContinue();
            }
        }
    }

    public RemoteDevice? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                ContinueCommand.NotifyCanExecuteChanged();


                ContinueCommandCanExecute = CanContinue();
            }
        }
    }

    public bool? AlwaysDoThis
    {
        get => _alwaysDoThis;
        set
        {
            if (SetProperty(ref _alwaysDoThis, value))
            {
                ContinueCommand.NotifyCanExecuteChanged();

                ContinueCommandCanExecute = CanContinue();
            }
        }
    }

    public bool ContinueCommandCanExecute
    {
        get => _continueCommandCanExecute;
        set => SetProperty(ref _continueCommandCanExecute, value);
    }


    private bool CanContinue()
    {
        switch (WhichOne)
        {
            case PromptDeviceSelectionResultType.PlayOnDevice:
                return SelectedDevice != OwnDevice && SelectedDevice is not null;
            case PromptDeviceSelectionResultType.PlayOnThisDevice:
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}