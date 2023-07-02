using CommunityToolkit.Mvvm.ComponentModel;
using Eum.Spotify.playlist4;
using LanguageExt;
using Wavee.UI.Client.Playlist.Models;
using Wavee.UI.User;

namespace Wavee.UI.ViewModel.Playlist;

public sealed class PlaylistViewModel : ObservableObject
{
    private readonly UserViewModel _user;
    private PlaylistRevisionId _revision;
    private string _id;
    private PlaylistTrackViewModel[] _tracks;

    public PlaylistViewModel(UserViewModel user)
    {
        _user = user;
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

    public PlaylistTrackViewModel[] Tracks
    {
        get => _tracks;
        set => SetProperty(ref _tracks, value);
    }
    public async Task Initialize(string id, CancellationToken cancellationToken)
    {
        Id = id;
        var client = _user.Client.Playlist;
        var playlist = await client.GetPlaylist(id, cancellationToken);
        Revision = playlist.Revision;
        Tracks = playlist.Tracks.Select(x => new PlaylistTrackViewModel(x)).ToArray();
    }
}

public sealed class PlaylistTrackViewModel : ObservableObject
{
    private WaveeUITrack? _track;

    public PlaylistTrackViewModel(WaveeUIPlaylistTrackInfo waveeUiPlaylistTrackInfo)
    {
        Uid = waveeUiPlaylistTrackInfo.Uid.IfNone(waveeUiPlaylistTrackInfo.Id);
        Id = waveeUiPlaylistTrackInfo.Id;
        AddedAt = waveeUiPlaylistTrackInfo.AddedAt;
        AddedBy = waveeUiPlaylistTrackInfo.AddedBy;
    }
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

    public bool Negate(bool b)
    {
        return !b;
    }
}

public class WaveeUITrack
{
}