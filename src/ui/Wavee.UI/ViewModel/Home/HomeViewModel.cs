using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Serilog;
using Wavee.Metadata.Home;
using Wavee.UI.Common;

namespace Wavee.UI.ViewModel.Home;

public sealed class HomeViewModel : ObservableObject
{
    private string? _greeting;
    private bool _loading;

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

    public ObservableCollection<HomeGroupSectionViewModel> Sections { get; } = new();

    public async Task Fetch(CancellationToken ct = default)
    {
        try
        {
            IsBusy = true;
            var client = SpotifyClient.Clients.Head().Value;
            var response = await client.Metadata.GetHomeView(TimeZoneInfo.Local, Option<CultureInfo>.None, ct);
            Greeting = response.Greeting;
            Sections.Clear();
            int sectionindex = 0;
            foreach (var section in response.Sections)
            {
                Sections.Add(new HomeGroupSectionViewModel
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
                            },
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
                            _ => null
                        }).Take((int)(sectionindex is 0 ? 6 : section.TotalCount)).Where(x=> x is not null).ToList()!,
                    Title = section.Title,
                    Subtitle = null,
                    Rendering = sectionindex > 0 ? HomeGroupRenderType.HorizontalStack : HomeGroupRenderType.Grid
                });
                sectionindex++;
            }

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