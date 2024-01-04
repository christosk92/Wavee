using Wavee.UI.ViewModels.Profile;

namespace Wavee.UI.Query.Contracts;

public sealed class GetProfileInformationQuery
    : IAuthenticatedQuery<ProfileViewModel>
{
    public ProfileContext Profile { get; set; }
}