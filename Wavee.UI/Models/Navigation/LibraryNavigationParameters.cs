using Wavee.Enums;

namespace Wavee.UI.Models.Navigation
{
    public readonly record struct LibraryNavigationParameters(
        string NavigateTo,
        bool Hearted,
        ServiceType ForService
    );
}
