using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.Domain.Playback;
using Wavee.UI.Features.Dialog.Queries;
using Wavee.UI.Features.Dialog.ViewModels;

namespace Wavee.UI.WinUI.Dialogs;

public sealed partial class DeviceSelectionDialog : ContentDialog
{
    public DeviceSelectionDialog(NoActiveDeviceSelectionDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();

        this.Closing += OnClosing;
    }

    private async void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        await Result;

        this.Bindings.StopTracking();
    }

    public Task<PromptDeviceSelectionResult> Result => ViewModel.Result;
    public NoActiveDeviceSelectionDialogViewModel ViewModel { get; set; }

    public double ToOpacity(bool? b)
    {
        if (b is null)
        {
            return 0.5;
        }

        return b.Value ? 1 : 0.5;
    }

    public bool NullableUnwrap(bool? b, bool b1)
    {
        return b ?? b1;
    }

    public Visibility IsEmpty(ObservableCollection<RemoteDevice> observableCollection)
    {
        var isEmpty = observableCollection.Count == 0;

        return isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    public bool? IsPlayHere(PromptDeviceSelectionResultType promptDeviceSelectionResultType)
    {
        return promptDeviceSelectionResultType == PromptDeviceSelectionResultType.PlayOnThisDevice;
    }

    public void SetPlayHere(bool? isChecked)
    {
        if (isChecked is true)
        {
            ViewModel.WhichOne= PromptDeviceSelectionResultType.PlayOnThisDevice;
        }
        else
        {
            ViewModel.WhichOne = PromptDeviceSelectionResultType.PlayOnDevice;
        }
    }

    public bool? IsOtherDevice(PromptDeviceSelectionResultType promptDeviceSelectionResultType)
    {
        return promptDeviceSelectionResultType == PromptDeviceSelectionResultType.PlayOnDevice;
    }

    public void SetOtherDevice(bool? isChecked)
    {
        if (isChecked is true)
        {
            ViewModel.WhichOne = PromptDeviceSelectionResultType.PlayOnDevice;
        }
        else
        {
            ViewModel.WhichOne = PromptDeviceSelectionResultType.PlayOnThisDevice;
        }
    }
}