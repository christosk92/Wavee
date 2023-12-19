using System.Collections.Immutable;
using System.Numerics;
using Eum.Spotify.playlist4;
using Mediator;
using Wavee.Spotify.Application.AudioKeys.QueryHandlers;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Common;
using Wavee.UI.Features.Playlists.Queries;

namespace Wavee.UI.Features.Playlists.QueryHandlers;

public sealed class GetUserPlaylistsQueryHandler : IQueryHandler<GetUserPlaylistsQuery, GetUserPlaylistsResult>
{
    private readonly ISpotifyClient _spotifyClient;

    public GetUserPlaylistsQueryHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async ValueTask<GetUserPlaylistsResult> Handle(GetUserPlaylistsQuery query, CancellationToken cancellationToken)
    {
        // TODO: Caching and diff based on cache.
        var selectedListContent = await _spotifyClient.Playlists.GetRootList(cancellationToken);


        return ParseItems(selectedListContent);
    }

    private static GetUserPlaylistsResult ParseItems(SelectedListContent selectedListContent)
    {
        Span<AbsUserPlaylistItem> output = new AbsUserPlaylistItem[selectedListContent.Contents.Items.Count];

        AbsUserPlaylistItem? currentFolder = null;
        List<AbsUserPlaylistItem>? currentFolderItems = null;
        int index = 0;
        foreach (var (item, metaItem) in selectedListContent.Contents.Items.Zip(selectedListContent.Contents.MetaItems))
        {
            if (item.Uri.StartsWith("spotify:start-group:"))
            {
                // Folder name is end of the uri.
                var split = item.Uri.Split(':');
                var folderName = split[split.Length - 1];
                //id is the last part of the uri before the folder name
                var id = item.Uri.Substring(0, item.Uri.Length - folderName.Length - 1);
                currentFolderItems = new List<AbsUserPlaylistItem>();
                currentFolder = new FolderUserPlaylistItem(currentFolderItems)
                {
                    Title = folderName,
                    Id = id
                };
            }
            else if (item.Uri.StartsWith("spotify:end-group:") && currentFolder is not null)
            {
                output[index++] = currentFolder;
                currentFolder = null;
                currentFolderItems = null;
            }
            else if (item.Uri.StartsWith("spotify:playlist:"))
            {
                var spotifyId = SpotifyId.FromUri(item.Uri);

                var images = ParseImages(metaItem.Attributes);
                if (metaItem.Attributes is null)
                {
                    continue;
                }
                var entity = new PlaylistUserPlaylistItem
                {
                    Id = spotifyId,
                    Title = metaItem.Attributes.Name,
                    Length = metaItem.Length,
                    Owner = metaItem.OwnerUsername,
                    Images = images ?? System.Array.Empty<SpotifyImage>(),
                    HasImages = images is not null,
                    Metadata = metaItem.Attributes.FormatAttributes.ToDictionary(x => x.Key,
                        x => x.Value),
                    Description = metaItem.Attributes.Description,
                    Revision = new BigInteger(metaItem.Revision.Span, true, true),
                };

                if (currentFolder is not null)
                {
                    currentFolderItems!.Add(entity);
                }
                else
                {
                    output[index++] = entity;
                }
            }
        }

        return new GetUserPlaylistsResult
        {
            Items = ImmutableArray.Create(output[..index])
        };
    }

    private static IReadOnlyCollection<SpotifyImage>? ParseImages(ListAttributes? metaItemAttributes)
    {
        if (metaItemAttributes is null) return null;
        if (metaItemAttributes.PictureSize.Count is 0)
        {
            if (!metaItemAttributes.HasPicture) return null;

            var picture = metaItemAttributes.Picture;
            // Convert to hex string
            var hex = SpotifyGetAudioKeyQueryHandler.ToBase16(picture.Span).ToLower();
            return ImmutableArray.Create(new SpotifyImage
            {
                Url = $"https://i.scdn.co/image/{hex}",
                Width = null,
                Height = null,
            });
        }

        Span<SpotifyImage> images = new SpotifyImage[metaItemAttributes.PictureSize.Count];
        for (int i = 0; i < metaItemAttributes.PictureSize.Count; i++)
        {
            var x = metaItemAttributes.PictureSize[i];
            ushort sizeAsPixels = x.TargetName switch
            {
                "default" => 300,
                "small" => 60,
                "large" => 640,
                "xlarge" => 720,
            };
            images[i] = new SpotifyImage
            {
                Url = x.Url,
                Width = sizeAsPixels,
                Height = sizeAsPixels,
            };
        }

        return ImmutableArray.Create(images);
    }
}