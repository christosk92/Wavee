using System.Globalization;
using LanguageExt;
using Wavee.Id;
using Wavee.UI.Client.Home;
using Wavee.UI.Common;
using Wavee.UI.ViewModel.Home;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyUIHomeClient : IWaveeUIHomeClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;
    public SpotifyUIHomeClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public async Task<WaveeHome> GetHome(string? filter, CancellationToken ct = default)
    {
        if (!_spotifyClient.TryGetTarget(out var client))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        bool isPodcastsFilter = false;
        var typeFilterType = Option<AudioItemType>.None;
        switch (filter)
        {
            case "Podcasts & Shows":
                typeFilterType = AudioItemType.PodcastEpisode | AudioItemType.PodcastShow;
                isPodcastsFilter = true;
                break;
            case "Music":
                typeFilterType = AudioItemType.Album
                                 | AudioItemType.Artist
                                 | AudioItemType.Playlist
                                 | AudioItemType.UserCollection
                                 | AudioItemType.Track;
                isPodcastsFilter = false;
                break;
        }

        var response =
            await Task.Run(() => client.Metadata.GetHomeView(typeFilterType, TimeZoneInfo.Local, Option<CultureInfo>.None, ct), ct);
        var greeting = response.Greeting;
        int sectionindex = 0;
        var output = new List<HomeGroupSectionViewModel>();
        foreach (var section in response.Sections)
        {
            var item = new HomeGroupSectionViewModel
            {
                Items = section.Items
                    .Select(c=> CardViewModel.From(c))
                    .Take((int)((sectionindex is 0 && !isPodcastsFilter) ? 6 : section.TotalCount))
                    .Where(x => x is not null).ToList()!,
                Title = section.Title,
                Subtitle = null,
                Rendering = sectionindex > 0 ? HomeGroupRenderType.HorizontalStack : HomeGroupRenderType.Grid
            };
            if (item.Items.Count == 0)
                continue;
            output.Add(item);
            sectionindex++;
        }

        var filters = new[]
        {
            "Music",
            "Podcasts & Shows"
        };

        return new WaveeHome
        {
            Filters = filters,
            Greeting = greeting,
            Sections = output
        };
    }
}