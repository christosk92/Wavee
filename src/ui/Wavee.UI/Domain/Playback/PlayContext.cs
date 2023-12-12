using Eum.Spotify.context;
using Eum.Spotify.playback;
using Wavee.Spotify.Common;
using Wavee.UI.Features.Album.ViewModels;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Library.ViewModels.Artist;

namespace Wavee.UI.Domain.Playback;

public class PlayContext
{
    internal readonly Context _spContext = new Context();
    internal PlayOrigin _playOrigin;
    internal PreparePlayOptions _playOptions = new PreparePlayOptions();

    private PlayContext() { }

    public static ArtistBuilder FromLibraryArtist(LibraryArtistViewModel artist) => new ArtistBuilder(artist, false);
    public class ArtistBuilder
    {
        internal readonly PlayContext _playContext;
        public ArtistBuilder(LibraryArtistViewModel artist, bool withAutoplay)
        {
            _playContext = new PlayContext();
            _playContext._spContext.Uri = artist.Id;
            _playContext._spContext.Url = string.Empty;
            _playContext._spContext.Metadata.Add("context_description", artist.Name);
            _playContext._spContext.Metadata.Add("disable-autoplay", withAutoplay.ToString().ToLower());
            foreach (var album in artist.Albums)
            {
                var uri = SpotifyId.FromUri(album.Id);
                _playContext._spContext.Pages.Add(new ContextPage
                {
                    PageUrl = "hm://artistplaycontext/v1/page/spotify/album/" + uri.ToBase62() + "/km",
                    Metadata =
                    {
                        {"page_uri", album.Id},
                        {"type", ((int)album.GroupType).ToString()}
                    }
                });
            }

            _playContext._playOrigin = new PlayOrigin()
            {
                DeviceIdentifier = string.Empty,
                FeatureIdentifier = "artist",
                FeatureVersion = "xpui_2023-12-04_1701707306292_36b715a",
                ViewUri = string.Empty,
                ExternalReferrer = string.Empty,
                ReferrerIdentifier = "my_library",
                FeatureClasses = { },
            };
            _playContext._playOptions = new PreparePlayOptions();
        }

        public DiscographyBuilder FromDiscography(PlayContextDiscographyGroupType discographyGroupType)
        {
            if (discographyGroupType is not PlayContextDiscographyGroupType.All)
            {
                var intType = discographyGroupType switch
                {
                    PlayContextDiscographyGroupType.Albums => DiscographyGroupType.Album,
                    PlayContextDiscographyGroupType.Singles => DiscographyGroupType.Single,
                    PlayContextDiscographyGroupType.Compilations => DiscographyGroupType.Compilation,
                    _ => throw new ArgumentOutOfRangeException(nameof(discographyGroupType), discographyGroupType,
                                               null)
                };
                var allpages = _playContext._spContext.Pages;
                var pages = allpages.Where(x => x.Metadata["type"] == ((int)intType).ToString());
                _playContext._spContext.Pages.Clear();
                foreach (var page in pages)
                {
                    _playContext._spContext.Pages.Add(page);
                }
            }
            return new DiscographyBuilder(this);
        }

        public class DiscographyBuilder
        {
            private readonly ArtistBuilder _builder;

            internal DiscographyBuilder(ArtistBuilder builder)
            {
                _builder = builder;
            }

            public AlbumBuilder StartWithAlbum(AlbumViewModel album)
            {
                var pageIndex = _builder
                    ._playContext
                    ._spContext
                    .Pages
                    .Select((x, i) => (x, i))
                    .FirstOrDefault(f => f.x.Metadata["page_uri"] == album.Id.ToString()).i;

                _builder
                    ._playContext._playOptions.SkipTo = new SkipToTrack
                    {
                        PageIndex = (ulong)pageIndex,
                    };
                _builder
                    ._playContext._playOptions.License = "premium";

                return new AlbumBuilder(_builder);
            }
        }
    }

    public class AlbumBuilder
    {
        private readonly ArtistBuilder _builder;

        internal AlbumBuilder(ArtistBuilder context)
        {
            _builder = context;
        }

        public TrackBuilder StartWithTrack(AlbumTrackViewModel track)
        {
            _builder._playContext._playOptions.SkipTo??= new SkipToTrack();
            _builder._playContext._playOptions.SkipTo.TrackIndex = (ulong)(track.Number -1);

            return new TrackBuilder(_builder._playContext);
        }
    }

    public class TrackBuilder
    {
        private readonly PlayContext _ctx;

        internal TrackBuilder(PlayContext ctx)
        {
            _ctx = ctx;
        }
        public PlayContext Build()
        {
            return _ctx;
        }
    }
}