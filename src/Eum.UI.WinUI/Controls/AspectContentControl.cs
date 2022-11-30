using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI.Controls
{
    public sealed class AspectContentControl : ContentControl
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(availableSize.Width, availableSize.Width * 1.35);
        }
    }
}