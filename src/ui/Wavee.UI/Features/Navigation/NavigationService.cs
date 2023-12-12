using CommunityToolkit.Mvvm.Input;
using Mediator;

namespace Wavee.UI.Features.Navigation;

public interface INavigationService
{
    bool CanGoNext { get; }
    bool CanGoBack { get; }

    RelayCommand GoNextCommand { get; }
    RelayCommand GoBackCommand { get; }
    Type CurrentPage { get;  }

    void Navigate<TViewModel>(object navigationStateParameters, TViewModel? overrideDataContext = default);

    event EventHandler<object> NavigatedTo;
}