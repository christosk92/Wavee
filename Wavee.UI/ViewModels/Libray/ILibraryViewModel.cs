using Wavee.Enums;

namespace Wavee.UI.ViewModels.Libray;

public interface ILibraryViewModel
{
    bool HeartedFilter
    {
        get;
        set;
    }
    bool HasHeartedFilter
    {
        get;
        set;
    }
    ServiceType? Service
    {
        get;
        set;
    }

    Task Initialize();
}