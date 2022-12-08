using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Eum.UWP.Controls
{
    public sealed class AspectContentControl : ContentControl
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(availableSize.Width, availableSize.Width * 1.35);
        }
    }
}
