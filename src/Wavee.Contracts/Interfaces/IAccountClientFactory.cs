using Wavee.Contracts.Interfaces.Clients;

namespace Wavee.Contracts.Interfaces;

public interface IAccountClientFactory
{
    IAccountClient Create();
}