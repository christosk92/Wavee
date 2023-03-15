using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.AudioImport;
using Wavee.UI.AudioImport.Database;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.AudioItems;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Playback.Impl;

namespace Wavee.UI.ViewModels.Artist;
public partial class ArtistRootViewModel :
    ObservableObject,
    INavigatable
{
    [ObservableProperty]
    private string? _artistName;

    [ObservableProperty] private TrackWithPlaycount[]? _topSongs;

    [ObservableProperty] private DiscographyGroup[]? _discography;

    private readonly CancellationTokenSource _fetchToken = new CancellationTokenSource();
    public async void OnNavigatedTo(object parameter)
    {
        var task = parameter switch
        {
            string artistName => GetLocalArtist(artistName),
            _ => default
        };
        await task;
    }

    private async Task GetLocalArtist(string artistName)
    {
        ArtistName = artistName;

        var db = Ioc.Default.GetRequiredService<IAudioDb>();
        var pb = Ioc.Default.GetRequiredService<IPlaycountService>();

        var artistTracks = await Task.Run(() => db.AudioFiles
            .Find(a => a.Artists.Contains(artistName))
            .ToList());

        var playcounts = pb
            .GetPlaycounts(artistTracks.Select(a => a.Path).ToArray());

        TopSongs = artistTracks
            .Select((a, i) =>
            {
                var playcount = playcounts[i];
                return (a, playcount);
            })
            .OrderByDescending(a => a.playcount)
            .Take(10)
            .Select((a, i) => new TrackWithPlaycount
            {
                Playcount = a.playcount,
                ViewModel = new TrackViewModel(i, a.a)
            })
            .ToArray();

        Discography = new DiscographyGroup[]
        {
            new DiscographyGroup
            {
                Title = "ALBUMS",
                CanSwitchTemplate = true,
                TemplateType = TemplateTypeOrientation.Grid,
                Items = artistTracks
                    .GroupBy(a => a.Album)
                    .Select(a => new AlbumViewModel(new LocalAlbum
                    {
                        Album = a.Key,
                        Artists = a.SelectMany(k => k.Artists).ToArray(),
                        Image = a.FirstOrDefault(a => !string.IsNullOrEmpty(a.ImagePath))?.ImagePath,
                        ServiceType = ServiceType.Local,
                        Tracks = a.Count()
                    }, new AsyncRelayCommand<IPlayContext>(p=> PlayerViewModel.Instance.PlayTask(p)), null))
                    .ToArray()
            }
        };
    }

    public void OnNavigatedFrom()
    {
        _fetchToken.Cancel();
        _fetchToken.Dispose();
    }

    public int MaxDepth
    {
        get;
    }
}

public record TrackWithPlaycount
{
    public TrackViewModel ViewModel
    {
        get;
        init;
    }
    public ulong Playcount
    {
        get;
        init;
    }
}

public partial class DiscographyGroup : ObservableObject
{
    [ObservableProperty] private TemplateTypeOrientation _templateType;

    public bool CanSwitchTemplate
    {
        get; init;
    }
    public string Title
    {
        get;
        init;
    }
    public AlbumViewModel[] Items
    {
        get; init;
    }

    [RelayCommand]
    public void SwitchTemplates()
    {

    }

}
public enum TemplateTypeOrientation
{
    Grid,
    VerticalStack,
    HorizontalStack
}
