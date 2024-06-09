using Microsoft.UI.Xaml;

namespace Wavee.UI.WinUI.Converters;

public static class Vis
{
    public static Visibility TrueThenVisible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility TrueThenCollapsed(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
}