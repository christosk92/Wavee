using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Helpers.Extensions;

public static class FrameExtensions
{
    public static object? GetPageViewModel(this Frame frame) => frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
}
