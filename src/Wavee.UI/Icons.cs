using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Wavee.UI;

public static class Icons
{
    public static IconElement SegoeFluent(string glyph) => new FontIcon
    {
        Glyph = glyph,
        FontFamily = new FontFamily("Segoe Fluent Icons")
    };

    public static IconElement MediaPlayer(string glyph) => new FontIcon
    {
        FontFamily =
            new Microsoft.UI.Xaml.Media.FontFamily("/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons"),
        Glyph = glyph
    };
}

public static class IconSourceExtension
{
    public static IconSource ToIconSource(this IconElement icon) => icon switch
    {
        FontIcon fontIcon => new FontIconSource
        {
            Glyph = fontIcon.Glyph,
            FontFamily = fontIcon.FontFamily
        },
    };
}