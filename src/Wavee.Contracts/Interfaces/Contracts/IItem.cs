using Wavee.Contracts.Common;

namespace Wavee.Contracts.Interfaces.Contracts;

public interface IItem
{
    string Id { get; }
    string Name { get; }
    UrlImage[] Images { get; }
}