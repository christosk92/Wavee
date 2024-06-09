namespace Wavee.UI.WinUI.Converters;

public static class Bool
{
    public static bool And(bool a, bool b) => a && b;
    public static bool Or(bool a, bool b) => a || b;
    public static bool Not(bool a) => !a;
}