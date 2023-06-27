using Wavee.UI.Client.Home;
using Wavee.UI.Client.Library;
using Wavee.UI.Client.Playback;

namespace Wavee.UI.Client;

public class WaveeUIClient : IDisposable
{
    private IDisposable _disposable;
    private readonly Func<IWaveeUIPlaybackClient> _playbackFactory;
    private readonly Func<IWaveeUIHomeClient> _homeFactory;
    private readonly Func<IWaveeUILibraryClient> _libraryFactory;
    public WaveeUIClient(SpotifyClient spotifyClient)
    {
        _disposable = spotifyClient;

        _homeFactory = () => new SpotifyUIHomeClient(spotifyClient);
        _playbackFactory = () => new SpotifyUIPlaybackClient(spotifyClient);
        _libraryFactory = () => new SpotifyUILibraryClient(spotifyClient);
    }

    public IWaveeUIHomeClient Home => _homeFactory();
    public IWaveeUIPlaybackClient Playback => _playbackFactory();
    public IWaveeUILibraryClient Library => _libraryFactory();

    public void Dispose()
    {
        _disposable.Dispose();
    }
}
