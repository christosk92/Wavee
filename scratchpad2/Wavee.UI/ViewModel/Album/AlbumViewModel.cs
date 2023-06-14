using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.Core.Ids;
using Wavee.UI.Core;
using Wavee.UI.Core.Contracts.Album;

namespace Wavee.UI.ViewModel.Album;

public sealed class AlbumViewModel : ObservableObject
{
    private string? _albumImage;
    private string? _albumType;
    private string? _albumName;
    private string? _albumReleaseYear;
    private string? _albumTrackCountString;
    private string? _albumDurationString;
    private IReadOnlyList<SpotifyDiscView> _discs;

    public string? AlbumImage
    {
        get => _albumImage;
        set => SetProperty(ref _albumImage, value);
    }

    public string? AlbumType
    {
        get => _albumType;
        set => SetProperty(ref _albumType, value);
    }

    public string? AlbumName
    {
        get => _albumName;
        set => SetProperty(ref _albumName, value);
    }

    public string? AlbumReleaseYear
    {
        get => _albumReleaseYear;
        set => SetProperty(ref _albumReleaseYear, value);
    }

    public string? AlbumTrackCountString
    {
        get => _albumTrackCountString;
        set => SetProperty(ref _albumTrackCountString, value);
    }

    public string? AlbumDurationString
    {
        get => _albumDurationString;
        set => SetProperty(ref _albumDurationString, value);
    }

    public IReadOnlyList<SpotifyDiscView> Discs
    {
        get => _discs;
        set => SetProperty(ref _discs, value);
    }

    public async Task Create(AudioId id, CancellationToken ct = default)
    {
        var album = await Global.AppState.Album.GetAlbumViewAsync(id, ct);
        if (!string.IsNullOrEmpty(AlbumImage))
        {
            AlbumImage = album.Cover;
        }
        AlbumName = album.Name;
        AlbumType = album.Type;
        AlbumReleaseYear = album.Year.ToString();
        AlbumTrackCountString = album.TrackCount.ToString();

        double durationMs = 0;
        foreach (var disc in album.Discs)
        {
            foreach (var track in disc.Tracks)
            {
                durationMs += track.Duration.TotalMilliseconds;
            }
        }
        var duration = TimeSpan.FromMilliseconds(durationMs);
        AlbumDurationString = FormatDurationAsString(duration);

        Discs = album.Discs;
    }

    private string? FormatDurationAsString(TimeSpan duration)
    {
        //for example 23:32 should be formatted as 23 minutes 32 seconds
        //01:23:32 should be formatted as 1 hour 23 minutes 32 seconds
        //01:01:23:32 should be formatted as 1 day 1 hour 23 minutes 32 seconds

        var days = duration.Days;
        var hours = duration.Hours;
        var minutes = duration.Minutes;
        var seconds = duration.Seconds;

        var sb = new StringBuilder();
        if (days > 0)
        {
            sb.Append($"{days} day{(days > 1 ? "s" : "")} ");
        }

        if (hours > 0)
        {
            sb.Append($"{hours} hour{(hours > 1 ? "s" : "")} ");
        }

        if (minutes > 0)
        {
            sb.Append($"{minutes} minute{(minutes > 1 ? "s" : "")} ");
        }

        if (seconds > 0)
        {
            sb.Append($"{seconds} second{(seconds > 1 ? "s" : "")} ");
        }

        return sb.ToString().Trim();
    }
}