using Wavee.Contracts.Enums;

namespace Wavee.Contracts.Interfaces.Contracts;

public interface IPinnableItem : IItem, IComparable<IPinnableItem>
{
    ItemType Type { get; }
}