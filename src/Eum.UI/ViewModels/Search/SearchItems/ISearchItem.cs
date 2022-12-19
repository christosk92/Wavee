using Eum.UI.Items;
using Eum.UI.ViewModels.Search.Patterns;

namespace Eum.UI.ViewModels.Search.SearchItems;

public interface ISearchItem
{
	string Name { get; }
	string Description { get; }
	ComposedKey Key { get; }
	string? Image { get; set; }
	string Category { get; }
    int CategoryOrder { get; }
    public ItemId Id { get; }
}