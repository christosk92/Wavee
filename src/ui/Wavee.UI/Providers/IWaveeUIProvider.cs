using System.Collections;
using System.Diagnostics;
using System.Windows.Input;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.NowPlaying;

namespace Wavee.UI.Providers;

public interface IWaveeUIProvider
{
    IWaveeUIAuthenticationProvider Authentication { get; }

    ValueTask Initialize();
    ValueTask InitializeOnAuthenticated();
}

public interface IWaveeUIAuthenticationProvider
{
    IWaveeUIProvider RootProvider { get; }
    IWaveeUIAuthenticatedProfile? AuthenticatedProfile { get; }
    event EventHandler<WaveeUIAuthenticationModule> AuthenticationRequested;
    event EventHandler AuthenticationDone;
}

public interface IWaveeUIAuthenticatedProfile
{
    IWaveeUIProvider Provider { get; }
    event EventHandler<WaveeUIPlaybackState>? PlaybackStateChanged;
    ValueTask<WaveeUIPlaybackState> ConnectToRemoteStateIfApplicable();

    Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryArtists(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter, CancellationToken cancellation);
    Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryAlbums(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter, CancellationToken cancellation);
    Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryTracks(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter, CancellationToken cancellation);
    Task<IReadOnlyCollection<LyricsLine>> GetLyricsFor(string id);

    Task<(string Dark, string Light)> ExtractColorFor(string url);

    Task<bool> ResumeRemoteDevice(bool waitForResponse);
    Task<bool> PauseRemoteDevice(bool waitForResponse);
    Task<bool> SkipPrevious(bool waitForResponse);
    Task<bool> SkipNext(bool waitForResponse);
    Task<bool> SeekTo(TimeSpan position, bool waitForResponse);
    Task<bool> SetShuffle(bool isShuffling, bool waitForResponse);
    Task<bool> GoToRepeatState(WaveeRepeatStateType nextRepeatStateType, bool waitForResponse);
    Task<bool> SetVolume(double oneToZero, bool waitForResponse);
    Task<WaveeAlbumViewModel> GetAlbum(string albumId, ICommand playCommand);
}


public readonly record struct WaveeUIPlaybackState(IWaveePlayableItem? Item,
    bool IsShuffling,
    WaveeRepeatStateType RepeatState,
    bool IsPaused,
    Seq<WaveeUIRemoteDevice> Devices,
    Seq<WaveePlaybackRestrictionType> Restrictions)
{
    public TimeSpan Position => PositionOffset + PositionSw.Elapsed;

    internal Stopwatch PositionSw { get; init; }
    internal TimeSpan PositionOffset { get; init; }
}

public readonly record struct WaveeUIRemoteDevice(string Id, DeviceType Type, string Name, Option<float> Volume, bool IsActive);