using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using Wavee.Metadata.Artist;
using Wavee.UI.Client.Album;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Shell;

namespace Wavee.UI.ViewModel.Album;

public sealed class AlbumViewModel : ObservableObject
{
    private readonly UserViewModel _user;
    private WaveeUIAlbumView? _album;
    private bool _saved;
    private AppTheme _theme;

    public AlbumViewModel(UserViewModel user)
    {
        _user = user;
    }

    public WaveeUIAlbumView? Album
    {
        get => _album;
        set
        {
            if (SetProperty(ref _album, value))
            {
                this.OnPropertyChanged(nameof(ImageColor));
                this.OnPropertyChanged(nameof(TrackCountString));
                this.OnPropertyChanged(nameof(DurationString));
                this.OnPropertyChanged(nameof(CulturedDateString));
            }
        }
    }
    public bool Saved
    {
        get => _saved;
        set => SetProperty(ref _saved, value);
    }

    public AppTheme Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                this.OnPropertyChanged(nameof(ImageColor));
            }
        }
    }

    public string? ImageColor =>
        Theme switch
        {
            AppTheme.Light => Album?.LightColor.IfNone(string.Empty),
            AppTheme.Dark => Album?.DarkColor.IfNone(string.Empty),
            _ => null
        };

    public string TrackCountString => FormatTrackCount(Album?.Discs ?? Array.Empty<WaveeUIAlbumDisc>());
    public string DurationString => CalculateDuration(Album?.Discs ?? Array.Empty<WaveeUIAlbumDisc>());
    public string CulturedDateString => GetCulturedDateString(Album);
    public bool SetImage { get; set; }

    private static string GetCulturedDateString(WaveeUIAlbumView? album)
    {
        if (album is null)
        {
            return string.Empty;
        }

        //january 1, 2021
        //January 2021
        //2021
        var precision = album.ReleaseDatePrecision;
        switch (precision)
        {
            case ReleaseDatePrecisionType.Unknown:
                return string.Empty;

            case ReleaseDatePrecisionType.Year:
                return album.ReleaseDate.Year.ToString();
            case ReleaseDatePrecisionType.Month:
                var month = album.ReleaseDate.ToString("MMMM");
                var year = album.ReleaseDate.Year;
                return $"{month} {year}";
            case ReleaseDatePrecisionType.Day:
                var day = album.ReleaseDate.Day;
                var month2 = album.ReleaseDate.ToString("MMMM");
                var year2 = album.ReleaseDate.Year;
                return $"{month2} {day}, {year2}";
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public async Task Fetch(string id)
    {
        Album = await _user.Client.Album.GetAlbum(id, CancellationToken.None);
        Saved = ShellViewModel.Instance.Library.InLibrary(id);
    }

    public void OnThemeChange(AppTheme theme)
    {
        Theme = theme;
    }

    public string CalculateDuration(WaveeUIAlbumDisc[] waveeUiAlbumDiscs)
    {
        var sum = waveeUiAlbumDiscs.Sum(f => f.Tracks.Sum(x => x.Duration.TotalMilliseconds));
        //1 hr 43 min 23 sec
        //4 min 23 sec
        var hours = sum / 3600000;
        var minutes = (sum % 3600000) / 60000;
        var seconds = (sum % 60000) / 1000;
        var result = new StringBuilder();
        if (hours >= 1)
        {
            var rounded = (int)hours;
            result.Append($"{rounded} hr ");
        }

        if (minutes >= 1)
        {
            var rounded = (int)minutes;
            result.Append($"{rounded} min ");
        }

        if (seconds >= 0)
        {
            var rounded = (int)seconds;
            result.Append($"{rounded} sec");
        }

        return result.ToString();
    }

    public string FormatTrackCount(WaveeUIAlbumDisc[] waveeUiAlbumDiscs)
    {
        var totalTracks = waveeUiAlbumDiscs.Sum(f => f.Tracks.Count);
        return totalTracks > 1 ? $"{totalTracks} tracks" : $"{totalTracks} track";
    }
}