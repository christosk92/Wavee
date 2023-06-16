using System.Text;
using System.Text.Json;
using System.Web;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.UI.Core.Contracts.Common;
using Wavee.UI.Core.Contracts.Search;

namespace Wavee.UI.Core.Sys.Live;

internal sealed class SpotifySearchClient : ISearchClient
{
    private readonly SpotifyClient _client;
    public SpotifySearchClient(SpotifyClient client)
    {
        _client = client;
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(string query, CancellationToken token)
    {
        if (string.IsNullOrEmpty(query))
            return Enumerable.Empty<SearchResult>();
        var mercury = _client.Mercury;
        const string search = "hm://searchview/km/v4/search/{0}?limit=10&entityVersion=2&catalogue=premium&country={1}&locale={2}";
        var url = string.Format(search, HttpUtility.UrlEncode(query), _client.CountryCode.IfNone("US"), _client.Config.Locale);

        var response = await mercury.Get(url, token);
        using var jsonDocument = JsonDocument.Parse(response.Payload);

        using var order = jsonDocument.RootElement.GetProperty("categoriesOrder").EnumerateArray();
        var results = jsonDocument.RootElement.GetProperty("results"); ;

        var output = new List<SearchResult>();
        foreach (var category in order)
        {
            var categoryResults = results.GetProperty(category.GetString());
            var items = categoryResults.GetProperty("hits").EnumerateArray();
            foreach (var item in items)
            {
                var type = item.GetProperty("type").GetString();
                var id = item.GetProperty("uri").GetString();
                var uri = AudioId.FromUri(id);
                var name = item.GetProperty("name").GetString();
                var imageUrl = item.GetProperty("image").GetProperty("url").GetString();
                var key = new SearchResultKey(uri, type switch
                {
                    "track" => SearchGroup.Track,
                    "album" => SearchGroup.Album,
                    "artist" => SearchGroup.Artist,
                    _ => SearchGroup.Unknown
                });
                var cardItem = new CardItem
                {
                    Id = uri,
                    Title = name,
                    ImageUrl = imageUrl,
                    Subtitle = string.Empty
                };
                output.Add(new SearchResult(key, cardItem));
            }
        }

        return output;
    }
}