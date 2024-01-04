using Tango.Types;

namespace Wavee.UI.Navigation;

/// <summary>
/// A frame facade is responsible for providing a frame to the navigation context.
/// </summary>
public interface IFrameFacade
{
    void NavigateTo(Type pageType, Option<object> viewModel, NavigationAnimationType animationType);

    event EventHandler<FrameNavigatedEventArgs> NavigatedTo;
}

public sealed class FrameNavigatedEventArgs : EventArgs
{
    public required Type ViewType { get; init; }
    public required object View { get; init; }
    public required Option<object> ViewModel { get; init; }
}

public enum NavigationAnimationType
{
    None,
    SlideFromLeft,
    SlideFromRight,
    DrillIn
}