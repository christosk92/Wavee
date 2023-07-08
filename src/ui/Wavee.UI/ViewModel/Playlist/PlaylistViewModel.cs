using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Serilog;
using Spotify.Metadata;
using System;
using System.Text;
using LanguageExt.UnsafeValueAccess;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;
using Wavee.UI.Client.Playlist.Models;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Playlist.Headers;

namespace Wavee.UI.ViewModel.Playlist;

public sealed class PlaylistViewModel : ObservableObject
{
    private readonly UserViewModel _user;
    private PlaylistRevisionId _revision;
    private string _id;
    private WaveeUIPlaylistTrackInfo[] _tracks;
    private IPlaylistHeader _header = new LoadingPlaylistHeader();
    private bool _tracksHaveDate;

    public PlaylistViewModel(UserViewModel user)
    {
        _user = user;
        WaitForTracks = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public PlaylistRevisionId Revision
    {
        get => _revision;
        set => SetProperty(ref _revision, value);
    }
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    public TaskCompletionSource<Unit> FetchedAllTracks { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<Unit> WaitForTracks { get; }

    public IPlaylistHeader Header
    {
        get => _header;
        set => this.SetProperty(ref _header, value);
    }

    public bool TracksHaveDate
    {
        get => _tracksHaveDate;
        set => this.SetProperty(ref _tracksHaveDate, value);
    }


    public async Task Initialize(string id, CancellationToken cancellationToken)
    {
        Id = id;
        var client = _user.Client.Playlist;
        var playlist = await client.GetPlaylist(id, cancellationToken);
        if (playlist.Header is RegularPlaylistHeader regularPlaylistHeader)
        {
            regularPlaylistHeader.TracksCountString.Value = playlist.Tracks.Length switch
            {
                0 => "No tracks",
                1 => "1 track",
                _ => $"{playlist.Tracks.Length} tracks"
            };
        }
        Revision = playlist.Revision;
        Header = playlist.Header;
        _tracks = playlist.Tracks;
        TracksHaveDate = _tracks.Any(x => x.AddedAt.IsSome);
        WaitForTracks.TrySetResult(Unit.Default);
        //Tracks = playlist.Tracks.Select(x => new PlaylistTrackViewModel(x)).ToArray();
        await FetchTracksOnlyForSorting(playlist.FutureTracks, cancellationToken);
    }

    private async Task FetchTracksOnlyForSorting(
        TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>> playlistFutureTracks,
        CancellationToken cancellationToken)
    {
        var trckUris = _tracks.Select(x => x.Id).ToArray();
        var batches = trckUris.Chunk(2000);
        foreach (var batch in batches)
        {
            const int maxRetries = 5;
            int retryCount = 0;
            while (true)
            {
                try
                {
                    var tracks = await _user.Client.ExtendedMetadata.GetTracks(batch, true, cancellationToken);
                    playlistFutureTracks.TrySetResult(tracks.Values.ToSeq());
                    if (Header is RegularPlaylistHeader regularPlaylistHeader)
                    {
                        regularPlaylistHeader.TracksDurationStirng.Value = CalculateDuration(tracks);
                    }

                    break;
                }
                catch (Exception e)
                {
                    retryCount++;
                    Log.Error(e,
                        "An error occurred while trying to fetch tracks. retrying in 2 secs. Try {try} out of {maxRetries}",
                        retryCount, maxRetries);
                    await Task.Delay(2000, cancellationToken);
                    if (retryCount - 1 == maxRetries)
                        break;
                    continue;
                }
            }
        }
    }
    public string CalculateDuration(
        Dictionary<string, Either<WaveeUIEpisode, WaveeUITrack>> data)
    {
        var duration = data.Sum(x => x.Value.Match(
            track => track.DurationMs,
            episode => episode.DurationMs));

        //1 hr 23 min 45 sec
        var hours = duration / 3600000;
        var minutes = (duration % 3600000) / 60000;
        var seconds = (duration % 60000) / 1000;
        var sb = new StringBuilder();
        if (hours > 0)
        {
            sb.Append(hours);
            sb.Append(" hr ");
        }

        if (minutes > 0)
        {
            sb.Append(minutes);
            sb.Append(" min ");
        }

        if (seconds > 0)
        {
            sb.Append(seconds);
            sb.Append(" sec");
        }

        return sb.ToString();
    }
    public Dictionary<string, PlaylistTrackViewModel> Generate(int offset, int limit) => _tracks.Skip(offset)
        .Take(limit).Select((x, i) => new PlaylistTrackViewModel(x, (ushort)(i + offset)))
        .ToDictionary(x => x.Id, x => x);
    public async Task FetchAndSetTracks(Dictionary<string, PlaylistTrackViewModel> fill,
        Action<Action> invokeOnUithread,
        CancellationToken cancellationToken)
    {
        try
        {
            var trackUris = fill.Keys.ToArray();
            var tracks = await _user.Client.ExtendedMetadata.GetTracks(trackUris, true, cancellationToken);

            invokeOnUithread(() =>
            {
                foreach (var track in _tracks)
                {
                    if (tracks.TryGetValue(track.Id, out var t))
                    {
                        _ = t.Match(
                            Left: episode => fill[episode.Id].Episode = episode,
                            Right: tr => fill[track.Id].Track = tr
                        );
                    }
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

public sealed class PlaylistTrackViewModel : ObservableObject
{
    private WaveeUITrack? _track;
    private WaveeUIEpisode _episode;

    public PlaylistTrackViewModel(WaveeUIPlaylistTrackInfo waveeUiPlaylistTrackInfo, ushort index)
    {
        Uid = waveeUiPlaylistTrackInfo.Uid.IfNone(waveeUiPlaylistTrackInfo.Id);
        Id = waveeUiPlaylistTrackInfo.Id;
        AddedAt = waveeUiPlaylistTrackInfo.AddedAt;
        AddedBy = waveeUiPlaylistTrackInfo.AddedBy;
        Index = index;
    }
    public bool HasDate => AddedAt.IsSome;
    public string Uid { get; }
    public string Id { get; }
    public Option<DateTimeOffset> AddedAt { get; }
    public Option<string> AddedBy { get; }

    public WaveeUITrack? Track
    {
        get => _track;
        set
        {
            if (SetProperty(ref _track, value))
            {
                this.OnPropertyChanged(nameof(Loading));
            }
        }
    }


    public bool Loading => Track is null;
    public ushort Index { get; }

    public WaveeUIEpisode Episode
    {
        get => _episode;
        set
        {
            if (SetProperty(ref _episode, value))
            {
                this.OnPropertyChanged(nameof(Loading));
            }
        }
    }

    public bool Negate(bool b)
    {
        return !b;
    }

    public string FormatTimestamp(Option<DateTimeOffset> dateTimeOffsets)
    {
        if (dateTimeOffsets.IsNone) return string.Empty;
        var dateTimeOffset = dateTimeOffsets.ValueUnsafe();

        // less than 10 seconds: "Just now"
        //less than 1 minute: "X seconds ago"
        //less than 1 hour: "X minutes ago" OR // "1 minute ago"
        //less than 1 day: "X hours ago" OR // "1 hour ago"
        //less than 1 week: "X days ago" OR // "1 day ago"
        //Exact date

        var totalSeconds = (int)DateTimeOffset.Now.Subtract(dateTimeOffset).TotalSeconds;
        var totalMinutes = totalSeconds / 60;
        var totalHours = totalMinutes / 60;
        var totalDays = totalHours / 24;
        var totalWeeks = totalDays / 7;
        return dateTimeOffset switch
        {
            _ when dateTimeOffset > DateTimeOffset.Now.AddMinutes(-1) => "Just now",
            _ when dateTimeOffset > DateTimeOffset.Now.AddHours(-1) =>
                $"{totalMinutes} minute{(totalMinutes > 1 ? "s" : "")} ago",
            _ when dateTimeOffset > DateTimeOffset.Now.AddDays(-1) =>
                $"{totalHours} hour{(totalHours > 1 ? "s" : "")} ago",
            _ when dateTimeOffset > DateTimeOffset.Now.AddDays(-7) =>
                $"{totalDays} day{(totalDays > 1 ? "s" : "")} ago",
            _ when dateTimeOffset > DateTimeOffset.Now.AddMonths(-1) =>
                $"{totalWeeks} week{(totalWeeks > 1 ? "s" : "")} ago",
            _ => GetFullMonthStr(dateTimeOffset)
        };

        static string GetFullMonthStr(DateTimeOffset d)
        {
            string fullMonthName =
                d.ToString("MMMM");
            return $"{fullMonthName} {d.Day}, {d.Year}";
        }
    }

    public string FormatDuration(int i)
    {
        var duration = TimeSpan.FromMilliseconds(i);
        //in minutes and seconds
        return duration.ToString("mm\\:ss");
    }
}

public class WaveeUITrack
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ITrackArtist[] Artists { get; set; }
    public TrackAlbum Album { get; set; }
    public int DurationMs { get; set; }
    public int TrackNumber { get; set; }
    public int DiscNumber { get; set; }
    public string AlbumName { get; set; }
    public CoverImage[] Covers { get; set; }
}

public class WaveeUIEpisode
{
    public CoverImage[] Covers { get; set; }
    public string Id { get; set; }
    public int DurationMs { get; set; }
}