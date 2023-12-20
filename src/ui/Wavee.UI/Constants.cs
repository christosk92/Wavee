using CommunityToolkit.Mvvm.Input;

namespace Wavee.UI;

public static class Constants
{
    public static IServiceProvider ServiceProvider { get; set; } = null!;

    public static RelayCommand<string> NavigationCommand { get; set; } = null!;
}