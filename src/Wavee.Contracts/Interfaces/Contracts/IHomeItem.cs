using Wavee.Contracts.Common;
using Wavee.Contracts.Models;

namespace Wavee.Contracts.Interfaces.Contracts;

public interface IHomeItem
{
    IItem Item { get; }
    ComposedKey Key { get; set; }
    string MediumImageUrl { get; }
    string DescriptionText { get; }
    HomeGroup Group { get; set; }
    int Order { get; set; }
    string Color { get; set; }
}