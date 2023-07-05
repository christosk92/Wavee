using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Serilog;
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
        Revision = playlist.Revision;
        Header = playlist.Header;
        _tracks = playlist.Tracks;
        TracksHaveDate = _tracks.Any(x => x.AddedAt.IsSome);
        WaitForTracks.TrySetResult(Unit.Default);
        //Tracks = playlist.Tracks.Select(x => new PlaylistTrackViewModel(x)).ToArray();
        await FetchTracksOnlyForSorting(playlist.FutureTracks, cancellationToken);
    }

    private async Task FetchTracksOnlyForSorting(TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>> playlistFutureTracks,
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
                            Left: episode => throw new NotImplementedException(),
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

    public bool Negate(bool b)
    {
        return !b;
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
}