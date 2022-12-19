using Eum.Connections.Spotify.Models.Users;
using Eum.UI.Items;
using Eum.UI.ViewModels.Search.Patterns;

namespace Eum.UI.ViewModels.Search.SearchItems;

public class SpotifySearchItem : ISearchItem
{
	public SpotifySearchItem(string title, string description, string image, SpotifyId id,  string category)
    {
        Description = description;
        Image = image;
        Id = id;
        Name = title;
        Category = category;
    }
	public SpotifyId Id { get; }
	public string Name { get; }
	public string Description { get; }
	public ComposedKey Key => new(Id.Id);
	public string? Image { get; set; }
	public string Category { get; }
}
