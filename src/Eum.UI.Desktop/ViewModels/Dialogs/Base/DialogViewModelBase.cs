using Eum.UI.ViewModels.NavBar;

namespace Eum.UI.ViewModels.Dialogs.Base;

/// <summary>
/// CommonBase class.
/// </summary>
public abstract partial class DialogViewModelBase : NavBarItemViewModel
{
    [AutoNotify] private bool _isDialogOpen;
}