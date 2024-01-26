using Wavee.UI.Providers;

namespace Wavee.UI.ViewModels;

public interface IHasProfileViewModel
{
    void AddFromProfile(IWaveeUIAuthenticatedProfile profile);
    void RemoveFromProfile(IWaveeUIAuthenticatedProfile profile);
}