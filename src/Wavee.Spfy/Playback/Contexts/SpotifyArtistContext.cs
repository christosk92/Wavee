using System.Security.Cryptography;
using Eum.Spotify.context;

namespace Wavee.Spfy.Playback.Contexts;

internal sealed class SpotifyArtistContext : SpotifyPagedContext
{
    public SpotifyArtistContext(Guid connectionId,
        Context context,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> createSpotifyStream)
        : base(connectionId, context, createSpotifyStream)
    {
    }

    private bool _filledPages;

    public override async ValueTask<bool> MoveToRandom()
    {
        // Shuffle ENTIRE context, if we dont have all pages in memory, we need to fetch them
        if (!_filledPages)
        {
            await FillAllPages();
            _filledPages = true;
        }

        // var randomPage = RandomNumberGenerator.GetInt32(0, base.PagesCache.Count);
        // var page = base.PagesCache.ElementAt(randomPage);
        // var randomTrack = RandomNumberGenerator.GetInt32(0, page.Tracks.Count);
        var allTracksCount = base.PagesCache.Sum(page => page.Tracks.Count);
        var randomTrack = RandomNumberGenerator.GetInt32(0, allTracksCount);

        var movedTo = await MoveTo(randomTrack);
        return movedTo;
    }

    protected override async ValueTask<IReadOnlyCollection<ContextPage>> GetAllPages()
    {
        var ctx = await ResolveContext();
        var pages = ctx.Pages;
        var pagesTasks = pages.Select(async (page, i) =>
        {
            if (page.Tracks.Count is 0)
            {
                return await base.ResolvePage(page.PageUrl);
            }

            return page;
        });
        
        var pagesResolved = await Task.WhenAll(pagesTasks);
        return pagesResolved;
    }
}