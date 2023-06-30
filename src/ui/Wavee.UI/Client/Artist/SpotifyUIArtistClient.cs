using System.Globalization;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Id;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;

namespace Wavee.UI.Client.Artist;

internal sealed class SpotifyUIArtistClient : IWaveeUIArtistClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;
    public SpotifyUIArtistClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public async Task<WaveeUIArtistView> GetArtist(string id, CancellationToken ct)
    {
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new ObjectDisposedException(nameof(SpotifyUIArtistClient));
        }

        var artistResponse = await spotifyClient.Metadata.GetArtistOverview(
            artistId: SpotifyId.FromUri(id),
            destroyCache: false,
            languageOverride: Option<CultureInfo>.None,
            ct: ct);

        return new WaveeUIArtistView(
            id: artistResponse.Id.ToString(),
            name: artistResponse.Profile.Name,
            avatarImage: artistResponse.Visuals.AvatarImages.FirstOrDefault(),
            headerImage: artistResponse.Visuals.HeaderImage,
            monthlyListeners: artistResponse.Statistics.MonthlyListeners,
            followers: artistResponse.Statistics.Followers,
            topTracks: artistResponse.Discography.TopTracks
                .Select(ToArtistTopTrackViewModel).ToArray(),
            discographyPages: ConvertToPaged(artistResponse.Id, artistResponse.Discography, spotifyClient)
        );
    }

    private ArtistTopTrackViewModel ToArtistTopTrackViewModel(ArtistTopTrack artistTopTrack, int i)
    {
        return new ArtistTopTrackViewModel(
            id: artistTopTrack.Id.ToString(),
            uid: artistTopTrack.Uid,
            name: artistTopTrack.Name,
            playcount: artistTopTrack.Playcount,
            duration: artistTopTrack.Duration,
            contentRating: artistTopTrack.ContentRating,
            artists: artistTopTrack.Artists.Cast<ITrackArtist>().ToArray(),
            albumId: artistTopTrack.Album.Id.ToString(),
            albumImages: artistTopTrack.Album.Images.Cast<ICoverImage>().ToArray(),
            index: (ushort)i
        );
    }

    private static PagedArtistDiscographyPage[] ConvertToPaged(
        SpotifyId id,
        ArtistDiscography artistResponseDiscography,
        SpotifyClient spotifyClient)
    {
        //Since in the new graphql endpoint, not all albums are returned, we need to get them
        //Albums -> Singles -> Compilations
        var albumsPage = CreatePage(id, ReleaseType.Album, artistResponseDiscography.Albums, spotifyClient);
        var singlesPage = CreatePage(id, ReleaseType.Single, artistResponseDiscography.Singles, spotifyClient);
        var compilationsPage = CreatePage(id, ReleaseType.Compilation, artistResponseDiscography.Compilations, spotifyClient);

        return new[]
        {
            albumsPage,
            singlesPage,
            compilationsPage
        };
    }

    private static PagedArtistDiscographyPage CreatePage(
        SpotifyId id,
        ReleaseType release,
        Option<ArtistDiscographyRelease>[] releases,
        SpotifyClient spotifyClient)
    {
        var weakRef = new WeakReference<SpotifyClient>(spotifyClient);

        static async Task<IEnumerable<ArtistDiscographyRelease>> GetPage(
            SpotifyId id,
            WeakReference<SpotifyClient> client,
            ReleaseType releaseType,
            System.Collections.Generic.HashSet<ArtistDiscographyRelease> fetchedReleases,
            int total,
            int offset, int limit, CancellationToken ct)
        {
            //check if we have the required page already in fetchedReleases
            var page = fetchedReleases.Skip(offset).Take(limit).ToArray();
            if (page.Length == limit)
            {
                return page;
            }

            //if not, we need to fetch it
            if (!client.TryGetTarget(out var spotifyClient))
            {
                throw new ObjectDisposedException(nameof(SpotifyUIArtistClient));
            }
            //check if we need less than total
            var missingCount = Math.Min(limit, total - page.Length);
            if (missingCount == 0)
            {
                return page;
            }
            var atOffset = offset + page.Length;

            var response = await spotifyClient.Metadata.GetArtistDiscography(
                artistId: id,
                type: releaseType,
                offset: atOffset,
                limit: missingCount,
                ct: ct);
            foreach (var item in response)
            {
                fetchedReleases.Add(item);
            }

            return page.Concat(response);
        }

        return new PagedArtistDiscographyPage(
            getReleases: (offset, limit, ct) => GetPage(
                id: id,
                client: weakRef,
                releaseType: release,
                fetchedReleases: releases.Where(f => f.IsSome).Select(x => x.ValueUnsafe()).ToHashSet(),
                total: releases.Length,
                offset: offset,
                limit: limit,
                ct: ct
            ));
    }

    // private static ArtistDiscographyReleaseViewModel ToViewModel(ArtistDiscographyRelease artistDiscographyRelease)
    // {
    //     throw new NotImplementedException();
    // }
}