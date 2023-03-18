namespace Wavee.UI.Interfaces.Services
{
    public interface INavigationService
    {
        bool NavigateTo(string? pageKey,
            object? parameter = null,
            bool clearNavigation = false,
            AnimationType animation = AnimationType.None);
        bool GoBack();
        public event SharedNavigatedEventHandler? Navigated;
        bool CanGoBack { get; }
        void SetFrame(object frame);
        void InvokePseudoNavigation(SharedNavigationEventArgs data);
    }

    public enum AnimationType
    {
        None,
        SlideLTR,
        SlideRTL,
        PopIn
    }
}
public delegate void SharedNavigatedEventHandler(object sender, SharedNavigationEventArgs e);

public record SharedNavigationEventArgs(Type SourcePageType, object? Parameter);