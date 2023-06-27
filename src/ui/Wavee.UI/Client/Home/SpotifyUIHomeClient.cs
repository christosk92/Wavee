using LanguageExt;
using static System.Collections.Specialized.BitVector32;
using System.Globalization;
using Wavee.Id;
using Wavee.Metadata.Home;
using Wavee.UI.Common;
using Wavee.UI.ViewModel.Home;

namespace Wavee.UI.Client.Home;

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
                    .Select(c => c switch
                    {
                        SpotifyCollectionItem collectionItem => new CardViewModel
                        {
                            Id = collectionItem.Id.ToString(),
                            Title = "Your Library",
                            Image = "\uEB52",
                            ImageIsIcon = true
                        } as ICardViewModel,
                        SpotifyPlaylistHomeItem playlistItem => new CardViewModel
                        {
                            Id = playlistItem.Id.ToString(),
                            Title = playlistItem.Name,
                            Subtitle = playlistItem.Description.IfNone($"Playlist by {playlistItem.OwnerName}"),
                            Image = playlistItem.Images.HeadOrNone().Map(x => x.Url).IfNone(string.Empty),
                            ImageIsIcon = false
                        },
                        SpotifyAlbumHomeItem albumItem => new CardViewModel
                        {
                            Id = albumItem.Id.ToString(),
                            Title = albumItem.Name,
                            Subtitle = albumItem.Artists.HeadOrNone().Map(x => x.Name).IfNone(string.Empty),
                            Image = albumItem.Images.HeadOrNone().Map(x => x.Url).IfNone(string.Empty),
                            Caption = "ALBUM",
                            ImageIsIcon = false
                        },
                        SpotifyArtistHomeItem artistItem => new CardViewModel
                        {
                            Id = artistItem.Id.ToString(),
                            Title = artistItem.Name,
                            Subtitle = "Artist",
                            IsArtist = true,
                            Image = artistItem.Images.HeadOrNone().Map(x => x.Url).IfNone(string.Empty),
                            ImageIsIcon = false
                        },
                        SpotifyPodcastEpisodeHomeItem podcastEpisode => new PodcastEpisodeCardViewModel
                        {
                            Id = podcastEpisode.Id.ToString(),
                            Title = podcastEpisode.Name,
                            Image = podcastEpisode.Images.OrderByDescending(x => x.Width.IfNone(0)).HeadOrNone()
                                .Map(x => x.Url).IfNone(string.Empty),
                            Duration = podcastEpisode.Duration,
                            Progress = podcastEpisode.Position,
                            PodcastDescription = podcastEpisode.Description.IfNone(string.Empty),
                            Show = podcastEpisode.PodcastName,
                            Started = podcastEpisode.Started,
                            ReleaseDate = podcastEpisode.ReleaseDate,
                        },
                        _ => null
                    }).Take((int)((sectionindex is 0 && !isPodcastsFilter) ? 6 : section.TotalCount))
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