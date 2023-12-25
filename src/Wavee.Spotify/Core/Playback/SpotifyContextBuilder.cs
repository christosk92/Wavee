using Eum.Spotify.context;
using Wavee.Core.Enums;
using Wavee.Core.Playback;
using Wavee.Interfaces;
using Wavee.Spotify.Core.Models.Common;

namespace Wavee.Spotify.Core.Playback;

public class SpotifyContextBuilder
{
    internal delegate Func<CancellationToken, Task<Context>> ContextFactory();

    private ContextFactory? _contextFactory;

    private readonly IWaveeSpotifyClient _client;

    private SpotifyContextBuilder(IWaveeSpotifyClient client)
    {
        _client = client;
    }

    public static SpotifyContextBuilder New(IWaveeSpotifyClient client)
    {
        return new(client);
    }

    public SpotifyArtistContextBuilder FromArtist(SpotifyId id)
    {
        if (id.Type is not AudioItemType.Artist)
            throw new ArgumentException("Id must be of type artist", nameof(id));

        _contextFactory = () =>
        {
            return async (ct) =>
            {
                var context = await _client.Context.ResolveContext(id.ToString(), ct);
                return context;
            };
        };

        return new(_contextFactory, _client);
    }

    public sealed class SpotifyArtistContextBuilder
    {
        internal delegate Func<Task<ContextPage?>> Page();

        private readonly ContextFactory _contextFactory;
        private readonly IWaveeSpotifyClient _client;
        private Page? _page;

        internal SpotifyArtistContextBuilder(ContextFactory contextFactory, IWaveeSpotifyClient client)
        {
            _contextFactory = contextFactory;
            _client = client;
        }

        public TrackSpecificBuilder FromTopTracks()
        {
            _page = () =>
            {
                return async () =>
                {
                    var context = await _contextFactory()(CancellationToken.None);
                    // top tracks is the first page
                    var page = context.Pages.FirstOrDefault();
                    return page;
                };
            };

            return new TrackSpecificBuilder(_page, _client);
        }
    }

    public sealed class TrackSpecificBuilder
    {
        private readonly SpotifyArtistContextBuilder.Page _page;
        private readonly IWaveeSpotifyClient _client;
        private Func<Task<WaveePlaybackItem[]>>? _items;

        internal TrackSpecificBuilder(SpotifyArtistContextBuilder.Page page, IWaveeSpotifyClient client)
        {
            _page = page;
            _client = client;
        }

        public FinalizedBuilder StartFromIndex(int index)
        {
            // TODO: Adapt to MediaSource factory
            _items = async () =>
            {
                var page = await _page()();
                var items = page!.Tracks.Skip(index).Select(item => new WaveePlaybackItem
                {
                    Factory = async () => await _client.Playback.CreateStream(SpotifyId.FromUri(item.Uri)),
                    Id = item.Uri
                }).ToArray();
                return items ?? Array.Empty<WaveePlaybackItem>();
            };
            return new FinalizedBuilder(_items);
        }
    }

    public sealed class FinalizedBuilder
    {
        private readonly Func<Task<WaveePlaybackItem[]>>? _items;

        public FinalizedBuilder(Func<Task<WaveePlaybackItem[]>>? items)
        {
            _items = items;
        }

        public WaveePlaybackList Build()
        {
            return WaveePlaybackList.Create(_items);
        }
    }
}