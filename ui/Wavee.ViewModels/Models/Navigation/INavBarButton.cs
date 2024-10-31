using Wavee.ViewModels.Models.Navigation;

// ReSharper disable once CheckNamespace
namespace Wavee.ViewModels;
public interface INavBarButton : INavBarItem
{
    Task Activate();
}
