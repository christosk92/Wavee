using DynamicData;
using Wavee.Enums;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Extensions;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models;

namespace Wavee.ViewModels.Service;

public sealed class SpotifyLibraryService : ILibraryService
{
    private readonly ISpotifyClient _client;

    public SpotifyLibraryService(ISpotifyClient client)
    {   
        _client = client;
    }

    public Task InitializeAsync()
    {
        return _client.LibraryClient.Initialize();
    }

    public IObservable<IChangeSet<LibraryItem, SpotifyId>> Library =>
        _client
            .LibraryClient
            .LibraryItems
            .Connect()
            .Transform(x => new LibraryItem(x.Id, Mapping[x.Id.ItemType], x.AddedAt, 
                x.Item?.ToWaveeItem()
                ??
                SpotifyItemExtensions.HandleUnknownItem(x.Id)));


    private static readonly Dictionary<SpotifyItemType, LibraryItemType> Mapping =
        new()
        {
            { SpotifyItemType.Local, LibraryItemType.LikedSongs },
            { SpotifyItemType.Album, LibraryItemType.SavedAlbums },
            { SpotifyItemType.Artist, LibraryItemType.FollowedArtists },
            { SpotifyItemType.Track, LibraryItemType.LikedSongs },
        };
}