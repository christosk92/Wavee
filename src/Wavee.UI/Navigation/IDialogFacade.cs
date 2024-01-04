using Tango.Types;

namespace Wavee.UI.Navigation;

public interface IDialogFacade
{
    Task<Unit> OpenDialog(Type dialogType, Option<object> viewModel);
}