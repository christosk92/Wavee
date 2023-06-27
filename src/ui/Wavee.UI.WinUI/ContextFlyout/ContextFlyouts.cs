using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.ContextFlyout;

public static class ContextFlyouts
{
    public static MenuFlyout BuildFlyout(string id)
    {
        return new MenuFlyout();
    }
}