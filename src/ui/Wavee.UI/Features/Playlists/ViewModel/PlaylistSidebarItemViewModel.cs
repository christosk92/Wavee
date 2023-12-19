using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.Spotify.Domain.Common;

namespace Wavee.UI.Features.Playlists.ViewModel;

public abstract class AbsPlaylistSidebarItemViewModel : ObservableObject
{
    public required string Id { get; init; }
}

public sealed class FolderSidebarItemViewModel : AbsPlaylistSidebarItemViewModel
{
    public required string Name { get; init; }
    public ObservableCollection<AbsPlaylistSidebarItemViewModel> Children { get; } = new();
}

public sealed class PlaylistSidebarItemViewModel : AbsPlaylistSidebarItemViewModel
{
    public required string Name { get; init; }
    public required IReadOnlyCollection<SpotifyImage> Images { get; init; }
    public required bool HasImages { get; init; }
    public required string? SmallestImage { get; init; }
    public required bool HasImage { get; init; }
    public required string Owner { get; init; }
    public required string? BigImage { get; init; }
    public required int Items { get; init; }
    public required string? Description { get; init; }
    public required BigInteger Revision { get; init; }
}