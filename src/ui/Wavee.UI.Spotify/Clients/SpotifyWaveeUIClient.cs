using Wavee.UI.Client;
using Wavee.UI.Client.Album;
using Wavee.UI.Client.Artist;
using Wavee.UI.Client.Home;
using Wavee.UI.Client.Library;
using Wavee.UI.Client.Lyrics;
using Wavee.UI.Client.Playback;
using Wavee.UI.Client.Previews;

namespace Wavee.UI.Spotify.Clients;

public class SpotifyWaveeUIClient : IWaveeUIClient, IDisposable
{
    private IDisposable _disposable;
    private readonly Func<IWaveeUIPlaybackClient> _playbackFactory;
    private readonly Func<IWaveeUIHomeClient> _homeFactory;
    private readonly Func<IWaveeUILibraryClient> _libraryFactory;
    private readonly Func<IWaveeUIAlbumClient> _albumFactory;
    private readonly Func<IWaveeUIPreviewClient> _previewFactory;
    private readonly Func<IWaveeUIArtistClient> _artistFactory;
    private readonly Func<IWaveeUILyricsClient> _lyricsFactory;
    public SpotifyWaveeUIClient(SpotifyClient spotifyClient)
    {
        _disposable = spotifyClient;

        _homeFactory = () => new SpotifyUIHomeClient(spotifyClient);
        _playbackFactory = () => new SpotifyUIPlaybackClient(spotifyClient);
        _libraryFactory = () => new SpotifyUILibraryClient(spotifyClient);
        _albumFactory = () => new SpotifyUIAlbumClient(spotifyClient);
        _previewFactory = () => new SpotifyUIPreviewClient(spotifyClient);
        _artistFactory = () => new SpotifyUIArtistClient(spotifyClient);
        _lyricsFactory = () => new SpotifyUILyricsClient(spotifyClient);
    }

    public IWaveeUIHomeClient Home => _homeFactory();
    public IWaveeUIPlaybackClient Playback => _playbackFactory();
    public IWaveeUILibraryClient Library => _libraryFactory();
    public IWaveeUIAlbumClient Album => _albumFactory();
    public IWaveeUIPreviewClient Previews => _previewFactory();
    public IWaveeUIArtistClient Artist => _artistFactory();
    public IWaveeUILyricsClient Lyrics => _lyricsFactory();

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
