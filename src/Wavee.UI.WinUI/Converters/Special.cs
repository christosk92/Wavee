using Microsoft.UI.Xaml;
using Wavee.UI.WinUI.Enums;

namespace Wavee.UI.WinUI.Converters;

public static class Special
{
    public static Visibility IsTextStyleAndSelected(TitleBarButtonType type, bool isSelected)
    {
        if (type is not TitleBarButtonType.Text)
        {
            return Visibility.Collapsed;
        }

        return isSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility IsNotTextStyleOrTextStyleAndNotSelected(TitleBarButtonType titleBarButtonType, bool b)
    {
        if (titleBarButtonType is TitleBarButtonType.Text)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }
}