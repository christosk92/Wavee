using Eum.Connections.Spotify.Models;

namespace Eum.UI.Services.Home
{
    public class GroupedHomeItem : List<ISpotifyItem>
    {
        public GroupedHomeItem(IEnumerable<ISpotifyItem> items) : base(items) { }
        public string Key { get; init; }
        public string Title { get; init; }
        public string? TagLine { get; init; }
    }
}
