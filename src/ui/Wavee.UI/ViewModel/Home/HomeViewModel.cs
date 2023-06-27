using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Serilog;
using Wavee.Id;
using Wavee.Metadata.Home;
using Wavee.UI.Common;

namespace Wavee.UI.ViewModel.Home;

public sealed class HomeViewModel : ObservableObject
{
    private string? _greeting;
    private bool _loading;
    private string[] _filters;
    private string? _selectedFilter;

    public bool Loading
    {
        get => _loading;
        set => SetProperty(ref _loading, value);
    }
    public string? Greeting
    {
        get => _greeting;
        set => SetProperty(ref _greeting, value);
    }

    public bool IsBusy
    {
        get => _loading;
        set => SetProperty(ref _loading, value);
    }

    public string[] Filters
    {
        get => _filters;
        set => SetProperty(ref _filters, value);
    }

    public string? SelectedFilter
    {
        get => _selectedFilter;
        set => SetProperty(ref _selectedFilter, value);
    }
    public ObservableCollection<HomeGroupSectionViewModel> Sections { get; } = new();

    public async Task Fetch(CancellationToken ct = default)
    {
        try
        {
            IsBusy = true;
            var client = SpotifyClient.Clients.Head().Value;

            //SelectedFilter -> AudioItemType

            bool isPodcastsFilter = false;
            var typeFilterType = Option<AudioItemType>.None;
            switch (SelectedFilter)
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
            // var typeFilterType = Option<AudioItemType>.None;
            // if (Enum.TryParse(SelectedFilter, true, out AudioItemType typeFilter))
            // {
            //     typeFilterType = typeFilter;
            // }
            var response = await Task.Run(() => client.Metadata.GetHomeView(typeFilterType,TimeZoneInfo.Local, Option<CultureInfo>.None, ct), ct);
            Greeting = response.Greeting;
            Sections.Clear();
            int sectionindex = 0;
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
                                Image = podcastEpisode.Images.OrderByDescending(x=> x.Width.IfNone(0)).HeadOrNone().Map(x => x.Url).IfNone(string.Empty), 
                                Duration = podcastEpisode.Duration,
                                Progress = podcastEpisode.Position,
                                PodcastDescription = podcastEpisode.Description.IfNone(string.Empty),
                                Show = podcastEpisode.PodcastName,
                                Started = podcastEpisode.Started,
                                ReleaseDate = podcastEpisode.ReleaseDate,
                            },
                            _ => null
                        }).Take((int)((sectionindex is 0 && !isPodcastsFilter)? 6 : section.TotalCount)).Where(x => x is not null).ToList()!,
                    Title = section.Title,
                    Subtitle = null,
                    Rendering = sectionindex > 0 ? HomeGroupRenderType.HorizontalStack : HomeGroupRenderType.Grid
                };
                if (item.Items.Count == 0) 
                    continue;
                Sections.Add(item);
                sectionindex++;
            }
            Filters = new[]
            {
                "Music",
                "Podcasts & Shows"
            };
            var oldFilter = SelectedFilter;
            SelectedFilter = string.Empty;
            SelectedFilter = oldFilter;

            IsBusy = false;
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Failed to fetch home view");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
//"   at System.Text.Json.JsonElement.GetProperty(String propertyName)\r\n   at Wavee.Metadata.Common.SpotifyItemParser.ParseFrom(JsonElement element) in C:\\Users\\chris-pc\\Desktop\\Wavee\\Wavee\\src\\lib\\Wavee\\Metadata\\Common\\SpotifyItemParser.cs:line 14\r\n   at Wavee.Metadata.Live.LiveSpotifyMetadataClient.<GetRecentlyPlayed>d__9.MoveNext() in C:\\Users\\chris-pc\\Desktop\\Wavee\\Wavee\\src\\lib\\Wavee\\Metadata\\Live\\LiveSpotifyMetadataClient.cs:line 90\r\n   at Wavee.Metadata.Live.LiveSpotifyMetadataClient.<GetHomeView>d__10.MoveNext() in C:\\Users\\chris-pc\\Desktop\\Wavee\\Wavee\\src\\lib\\Wavee\\Metadata\\Live\\LiveSpotifyMetadataClient.cs:line 111\r\n   at Wavee.UI.ViewModel.Home.HomeViewModel.<Fetch>d__14.MoveNext() in C:\\Users\\chris-pc\\Desktop\\Wavee\\Wavee\\src\\ui\\Wavee.UI\\ViewModel\\Home\\HomeViewModel.cs:line 41"