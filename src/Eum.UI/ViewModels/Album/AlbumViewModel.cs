using System.Drawing;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows.Input;
using ColorThiefDotNet;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Models.Artists.Discography;
using Eum.Enums;
using Eum.Spotify.metadata;
using Eum.UI.Helpers;
using Eum.UI.Items;
using Eum.UI.Services.Albums;
using Eum.UI.Services.Artists;
using Eum.UI.ViewModels.Artists;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Playback;
using Eum.UI.ViewModels.Playlists;
using Eum.UI.ViewModels.Settings;
using Nito.AsyncEx;
using ReactiveUI;

namespace Eum.UI.ViewModels.Album;

[INotifyPropertyChanged]
public sealed partial class AlbumViewModel : INavigatable, IGlazeablePage
{
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isSaved;
    [ObservableProperty] private EumAlbum? _album;
    [ObservableProperty] private string _imagePath;
    [ObservableProperty] private int _trackCount;
    [ObservableProperty] private TimeSpan _totalTrackDuration;
    [ObservableProperty] private EumDiscViewModel[] _discs;
    [ObservableProperty] private bool _isGrouped;
    private string _albumImageOriginal;
    public ItemId Id { get; init; }

    public async void OnNavigatedTo(object parameter)
    {
        var provider = Ioc.Default.GetRequiredService<IAlbumProvider>();
        var main = Ioc.Default.GetRequiredService<MainViewModel>();
        var user = main.CurrentUser.User;
        RecheckIsSaved();

        main.PlaybackViewModel.PlayingItemChanged += PlaybackOnPlayingItemChanged;
        PlaybackOnPlayingItemChanged(main.PlaybackViewModel, main.PlaybackViewModel.Item.Id);

        user.LibraryProvider.CollectionUpdated += LibraryProviderOnCollectionUpdated;

        Album =
            await Task.Run(async () => await provider.GetAlbum(Id, "en_us"));
        _albumImageOriginal = Album.Images.FirstOrDefault().Id;
        TotalTrackDuration = TimeSpan.FromMilliseconds(Album.Discs.Sum(z => z.Sum(a => a.Duration)));
        var str = Album.Images.FirstOrDefault()?.ImageStream;
        if (str != null)
        {
            using var image = await str;
            //save to fs
            var id = _album.Name;
            var filePath = Path.GetTempFileName();
            using var fs = File.OpenWrite(filePath);
            await image.CopyToAsync(fs);

            ImagePath = filePath;
        }

        Discs = Album.Discs
            .Select(a => new EumDiscViewModel(a.Select((j, i) => new AlbumTrackViewModel(j, i)
            {
                IsSaved = user.LibraryProvider.IsSaved(new ItemId(j.Uri.Uri)),
            }))
            {
                Key = $"Disc {a.DiscNumber}",
            }).ToArray();
        IsGrouped = Discs.Length > 1;
        TrackCount = Discs.Sum(a => a.Count);
        _waitForAlbum.Set();
    }
    public void RecheckIsSaved()
    {
        var user = Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User;
        IsSaved = user.LibraryProvider.IsSaved(Id);
    }
    public void OnNavigatedFrom()
    {
        var main = Ioc.Default.GetRequiredService<MainViewModel>();
        main.PlaybackViewModel.PlayingItemChanged -= PlaybackOnPlayingItemChanged;

        main.CurrentUser.User.LibraryProvider.CollectionUpdated -= LibraryProviderOnCollectionUpdated;
    }
    private void LibraryProviderOnCollectionUpdated(object? sender, (EntityType Type, IReadOnlyList<CollectionUpdate> Ids) e)
    {
        if (e.Type is EntityType.Album or EntityType.Track)
        {
            foreach (var discographyGroup in Discs)
            {
                foreach (var discographyTrackViewModel in discographyGroup)
                {
                    var updatedOrNahh = e.Ids.FirstOrDefault(a => a.Id.Uri == discographyTrackViewModel.Id.Uri);
                    if (updatedOrNahh != null)
                    {
                        discographyTrackViewModel.IsSaved = !updatedOrNahh.Removed;
                    }
                }
            }

            RecheckIsSaved();
        }
    }

    public int MaxDepth { get; }

    public bool ShouldSetPageGlaze => Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.ThemeService
        .Glaze == "Page Dependent";

    public ICommand SortCommand { get; }

    private readonly AsyncManualResetEvent _waitForAlbum = new();

    public async ValueTask<string> GetGlazeColor(AppTheme theme, CancellationToken ct = default)
    {
        await _waitForAlbum.WaitAsync(ct);
        if (Album.Images.Any())
        {
            try
            {
                var colorsClient = Ioc.Default.GetRequiredService<IExtractedColorsClient>();
                var uri = new Uri(_albumImageOriginal);
                var color = await
                    colorsClient.GetColors(uri.ToString(), ct);
                switch (theme)
                {
                    case AppTheme.Dark:
                        return color[ColorTheme.Dark];
                    case AppTheme.Light:
                        return color[ColorTheme.Light];
                    default:
                        throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
                }
            }
            catch (Exception ex)
            {
                using var fs = await Album.Images.First().ImageStream;
                fs.Position = 0;
                using var bmp = new Bitmap(fs);
                var colorThief = new ColorThief();
                var c = colorThief.GetPalette(bmp);

                return c[0].Color.ToHexString();
            }
        }

        return string.Empty;
    }
    private void PlaybackOnPlayingItemChanged(object? sender, ItemId e)
    {
        if ((sender is PlaybackViewModel p))
        {
            if (p.Item.Context.Equals(Id))
            {
                if (_isPlaying != true)
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        IsPlaying = true;
                    });
                }
            }
            else
            {
                if (_isPlaying != false)
                {
                    IsPlaying = false;
                }
            }
        }
    }

}

public class EumDiscViewModel : List<AlbumTrackViewModel>
{
    public EumDiscViewModel(IEnumerable<AlbumTrackViewModel> t) : base(t)
    {

    }
    public string Key { get; init; }
}
[INotifyPropertyChanged]
public partial class AlbumTrackViewModel : IIsPlaying
{
    [ObservableProperty] private bool _isSaved;
    public AlbumTrackViewModel(DiscographyTrackRelease discographyTrackRelease, int i)
    {
        Track = discographyTrackRelease;
        FtArtists = discographyTrackRelease.Artists
            .Select(a => new IdWithTitle
            {
                Id = new ItemId(a.Uri.Uri),
                Title = a.Name
            }).ToArray();
        Index = i;
    }
    public int Index { get; }
    public IdWithTitle[] FtArtists { get; }

    public DiscographyTrackRelease Track { get; }

    public ItemId Id => new ItemId(Track.Uri.Uri);

    public bool IsPlaying()
    {
        return Ioc.Default.GetRequiredService<MainViewModel>()
            .PlaybackViewModel?.Item?.Id == Id;
    }

    public bool WasPlaying => _wasPlaying;

    private bool _wasPlaying;

    public event EventHandler<bool>? IsPlayingChanged;
    public void ChangeIsPlaying(bool isPlaying)
    {
        if (_wasPlaying == isPlaying) return;

        _wasPlaying = isPlaying;
        IsPlayingChanged?.Invoke(this, isPlaying);
    }
}