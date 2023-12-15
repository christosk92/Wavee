using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Wavee.UI.Features.Playlists.Queries;

namespace Wavee.UI.Features.Playlists.ViewModel;

public sealed class PlaylistsViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    public PlaylistsViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ObservableCollection<AbsPlaylistSidebarItemViewModel> PlaylistViewModels { get; } = new();
    public async Task Initialize()
    {
        try
        {
            var userLists = await Task.Run(async () => await _mediator.Send(new GetUserPlaylistsQuery()));
            PlaylistViewModels.Clear();
            foreach (var x in userLists.Items)
            {
                var viewmodel = CreateViewModel(x);
                PlaylistViewModels.Add(viewmodel);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private AbsPlaylistSidebarItemViewModel CreateViewModel(AbsUserPlaylistItem absUserPlaylistItem)
    {
        switch (absUserPlaylistItem)
        {
            case FolderUserPlaylistItem folder:
                {
                    var folderViewModel = new FolderSidebarItemViewModel
                    {
                        Id = folder.Id,
                        Name = folder.Title,
                    };
                    foreach (var items in folder.Items)
                    {
                        var child = CreateViewModel(items);
                        folderViewModel.Children.Add(child);
                    }

                    return folderViewModel;
                }
            case PlaylistUserPlaylistItem playlist:
                {
                    var smallestImage = playlist.Images.OrderBy(x => x.Height).FirstOrDefault();
                    var bigImage = playlist.Images.OrderByDescending(x => x.Height).FirstOrDefault();
                    var playlistViewModel = new PlaylistSidebarItemViewModel
                    {
                        Id = playlist.Id.ToString(),
                        Name = playlist.Title,
                        Images = playlist.Images,
                        HasImages = playlist.HasImages,
                        SmallestImage = smallestImage.Url,
                        HasImage = !string.IsNullOrEmpty(smallestImage.Url),
                        Owner = playlist.Owner,
                        BigImage = bigImage.Url,
                        Items = playlist.Length,
                        Description = playlist.Description,
                    };

                    return playlistViewModel;
                }
        }

        throw new NotImplementedException();
    }
}