using Eum.UI.Stage;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Eum.UI.Helpers;
using Eum.UI.WinUI.Views.Profile.Create;
using Eum.Users;
using Microsoft.UI.Xaml;

namespace Eum.UI.WinUI.Helper;
class DialogHelper : IDialogHelper
{
    public async Task<object> ShowStageManagerAsDialogAsync(StageManager stageManager)
    {
        //App.Window.Content.XamlRoot
        var dialog = new ContentDialog
        {
            VerticalContentAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = stageManager.WizardType switch
            {
                WizardType.Pips => new PipsStageView(stageManager)
            },
            XamlRoot = App.MWindow.Content.XamlRoot
        };
        object? result = default;
        stageManager.RegisterCompletedCallback((r) =>
        {
            result = r;
            dialog.Hide();
        });
        await dialog.ShowAsync();
        return result;
    }

    // public async Task<bool> AuthenticateProfileDialogAsync(EumUser objUser)
    // {
    //     var dialog = new ContentDialog
    //     {
    //         VerticalContentAlignment = VerticalAlignment.Stretch,
    //         HorizontalContentAlignment = HorizontalAlignment.Stretch,
    //         PrimaryButtonText = "Authenticate", 
    //         PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
    //         SecondaryButtonText = "Cancel",
    //         XamlRoot = App.MWindow.Content.XamlRoot
    //     };
    //     var enterPwdDialog = new EnterPasswordDialog(objUser, dialog);
    //     await dialog.ShowAsync();
    //     return enterPwdDialog.SuccessFullAuthentication;
    // }
}