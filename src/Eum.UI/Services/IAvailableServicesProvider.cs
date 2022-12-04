using Eum.UI.Items;

namespace Eum.UI.Services
{
    public interface IAvailableServicesProvider
    {
        ServiceType[] AvailableServices { get; }
    }
}
