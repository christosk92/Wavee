using System.Collections.Immutable;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Playlists;
using Wavee.Spotify.Domain.Tracks;
using Wavee.UI.Domain;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Features.Artist.Queries;

namespace Wavee.UI.Features.Artist.QueryHandlers;


public sealed class GetArtistViewQueryHandler : IQueryHandler<GetArtistViewQuery, ArtistViewResult>
{
    private readonly ISpotifyClient _spotifyClient;

    public GetArtistViewQueryHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async ValueTask<ArtistViewResult> Handle(GetArtistViewQuery query, CancellationToken cancellationToken)
    {
        var artist = await _spotifyClient.Artist.GetAsync(SpotifyId.FromUri(query.Id), cancellationToken);

        return new ArtistViewResult
        {
            Id = artist.Id.ToString(),
            Name = artist.Profile.Name,
            HeaderImageUrl = artist.Visuals.HeaderImage?.Url,
            ProfilePictureImageUrl = artist.Visuals.AvatarImage.FirstOrDefault().Url,
            Followers = artist.Stats.Followers,
            MonthlyListeners = artist.Stats.MonthlyListeners,
            TopTracks = artist.Discography.TopTracks.Select(x => new ArtistTopTrackEntity
            {
                SmallImage = x.Images.MinBy(x => x.Height)
                    .Url,
                Playcount = x.Playcount,
                Id =  x.Id.ToString(),
                Name = x.Name,
                Artists = x.Artists.Select(f=> new SimpleArtistEntity
                {
                    Id = f.Uri.ToString(),
                    Name = f.Name,
                    Images = f.Images
                }).ToImmutableArray(),
                Images = x.Images,
                Duration = x.Duration,
            }).ToArray(),
            LatestRelease = artist.Discography.LatestRelease is not null
                ? BuildAlbum(artist.Discography.LatestRelease)
                : null,
            Discography = BuildDiscography(artist.Discography),
            RelatedContent = BuildRelatedContent(artist.Related)
        };
    }

    private static ArtistViewDiscographyGroup[] BuildDiscography(SpotifyArtistViewDiscography artistDiscography)
    {
        var albums = BuildDiscographyGroup(artistDiscography.Albums, DiscographyGroupType.Album);
        var singles = BuildDiscographyGroup(artistDiscography.Singles, DiscographyGroupType.Single);
        var compilations = BuildDiscographyGroup(artistDiscography.Compilations, DiscographyGroupType.Compilation);
        var appearsOn = BuildDiscographyGroup(artistDiscography.PopularReleases, DiscographyGroupType.PopularRelease);
        return new[] { albums, singles, compilations, appearsOn };
    }

    private static ArtistViewDiscographyGroup BuildDiscographyGroup(SpotifyArtistDiscographyGroup<SpotifySimpleAlbum> artistDiscographyAlbums, DiscographyGroupType type)
    {
        var totalAlbums = artistDiscographyAlbums.Total;
        Span<ArtistViewDiscographyItem> albumsOutput = new ArtistViewDiscographyItem[totalAlbums];
        var hasItemsLength = artistDiscographyAlbums.Items.Count;
        for (int i = 0; i < totalAlbums; i++)
        {
            //Check if we HAVE the album 
            if (i < hasItemsLength)
            {
                var album = artistDiscographyAlbums.Items.ElementAt(i);
                albumsOutput[i] = new ArtistViewDiscographyItem
                {
                    HasValue = true,
                    Album = BuildAlbum(album)
                };
            }
            else
            {
                //We don't have the album, so we need to fetch it
                albumsOutput[i] = new ArtistViewDiscographyItem
                {
                    HasValue = false,
                    Album = null
                };
            }
        }

        return new ArtistViewDiscographyGroup
        {
            Total = totalAlbums,
            Items = ImmutableArray.Create(albumsOutput),
            Type = type
        };
    }

    private static SimpleAlbumEntity BuildAlbum(SpotifySimpleAlbum album)
    {
        return new SimpleAlbumEntity
        {
            Id = album.Uri.ToString(),
            Name = album.Name,
            Images = album.Images,
            TracksCount = album.TotalTracks,
            Year = (ushort?)album.ReleaseDate.Year,
            Type = album.Type
        };
    }

    private static ArtistViewRelatedGroup[] BuildRelatedContent(SpotifyArtistViewRelatedContent artistRelated)
    {
        var artists = BuildRelatedGroup(artistRelated.RelatedArtists, BuildArtist, RelatedGroupType.Artist);
        var appearsOn = BuildRelatedGroup(artistRelated.AppearsOn, BuildAlbum, RelatedGroupType.AppearsOnAlbum);
        var discoveredOn = BuildRelatedGroup(artistRelated.DiscoveredOn, BuildPlaylist, RelatedGroupType.DiscoveredInPlaylist);
        var featuredIn = BuildRelatedGroup(artistRelated.FeaturedIn, BuildPlaylist, RelatedGroupType.FeaturedInPlaylist);

        return new[] { artists, appearsOn, discoveredOn, featuredIn };
    }

    private static IArtistRelatedItem BuildPlaylist(SpotifySimplePlaylist arg)
    {
        return new SimplePlaylistEntity
        {
            Id = arg.Uri.ToString(),
            Name = null,
            Images = null
        };
    }

    private static IArtistRelatedItem BuildArtist(SpotifySimpleArtist arg)
    {
        return new SimpleArtistEntity
        {
            Name = arg.Name,
            Id = arg.Uri.ToString(),
            Images = arg.Images
        };
    }

    private static ArtistViewRelatedGroup BuildRelatedGroup<T>(SpotifyArtistDiscographyGroup<T> items,
        Func<T, IArtistRelatedItem> parser,
        RelatedGroupType group)
    {
        var total = items.Total;
        Span<ArtistViewRelatedItem> output = new ArtistViewRelatedItem[total];
        var hasItemsLength = items.Items.Count;
        for (int i = 0; i < total; i++)
        {
            //Check if we HAVE the album 
            if (i < hasItemsLength)
            {
                var item = items.Items.ElementAt(i);
                output[i] = new ArtistViewRelatedItem
                {
                    HasValue = true,
                    Item = parser(item),
                };
            }
            else
            {
                //We don't have the album, so we need to fetch it
                output[i] = new ArtistViewRelatedItem
                {
                    HasValue = false,
                    Item = default
                };
            }
        }

        return new ArtistViewRelatedGroup
        {
            Total = total,
            Items = ImmutableArray.Create(output),
            Type = group
        };
    }
}