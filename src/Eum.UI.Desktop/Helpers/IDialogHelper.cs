using Eum.UI.Stage;
using Eum.Users;

namespace Eum.UI.Helpers
{
    public interface IDialogHelper
    {
        Task<object?> ShowStageManagerAsDialogAsync(StageManager stageManager);
        // Task<bool> AuthenticateProfileDialogAsync(EumUser objUser);
    }
}
