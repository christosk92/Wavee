using Wavee.ViewModels.Enums;

namespace Wavee.ViewModels.Infrastructure;

public interface IApplicationSettings
{
    WindowState WindowState { get; set; }
    bool Oobe { get; set; }
}