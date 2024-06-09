using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;

namespace Wavee.UI.FakeService;

public sealed class FakeSpotifyClientFactory : IAccountClientFactory
{
    public IAccountClient Create()
    {
        return new FakeSpotifyClient();
    }
}