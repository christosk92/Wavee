using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Serilog;
using Wavee.Id;
using Wavee.Metadata.Home;
using Wavee.UI.Common;
using Wavee.UI.User;

namespace Wavee.UI.ViewModel.Home;

public sealed class HomeViewModel : ObservableObject
{
    private readonly UserViewModel _user;
    private string? _greeting;
    private bool _loading;
    private string[] _filters;
    private string? _selectedFilter;

    public HomeViewModel(UserViewModel user)
    {
        _user = user;
    }

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
            Sections.Clear();
            var home = await _user.Client.Home.GetHome(SelectedFilter, ct);

            Greeting = home.Greeting;
            Filters = home.Filters;
            foreach (var item in home.Sections)
            {
                Sections.Add(item);
            }

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