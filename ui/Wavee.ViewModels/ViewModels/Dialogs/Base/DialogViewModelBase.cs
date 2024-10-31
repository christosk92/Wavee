using Wavee.ViewModels.ViewModels.Navigation;

namespace Wavee.ViewModels.ViewModels.Dialogs.Base;

/// <summary>
/// CommonBase class.
/// </summary>
public abstract partial class DialogViewModelBase : RoutableViewModel
{
    [AutoNotify] private bool _isDialogOpen;
}