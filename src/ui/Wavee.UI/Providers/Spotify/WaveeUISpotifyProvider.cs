using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Threading.Tasks.Sources;
using System.Windows.Input;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Spfy;
using Wavee.Spfy.Items;
using Wavee.Spfy.Remote;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.NowPlaying;
using static LanguageExt.Prelude;
namespace Wavee.UI.Providers.Spotify;

public sealed class WaveeUISpotifyProvider : IWaveeUIProvider
{
    private readonly WaveeUISpotifyAuthProvider _authProvider;
    public WaveeUISpotifyProvider(ISecureStorage secureStorage, WaveePlayer player)
    {
        _authProvider = new WaveeUISpotifyAuthProvider(this);

        SpotifyClient = new WaveeSpotifyClient(_authProvider.OpenBrowser, secureStorage, NullLogger.Instance, player);
    }

    public IWaveeUIAuthenticationProvider Authentication => _authProvider;
    internal WaveeSpotifyClient SpotifyClient { get; }
    public ValueTask Initialize()
    {
        return _authProvider.Initialize();
    }
    public ValueTask InitializeOnAuthenticated()
    {
        return ValueTask.CompletedTask;
    }
}

public class WaveeUISpotifyAuthProvider : IWaveeUIAuthenticationProvider
{
    private readonly WaveeUISpotifyProvider _rootProvider;
    private WaveeUISpotifyProfile? _spotifyProfile;

    public WaveeUISpotifyAuthProvider(WaveeUISpotifyProvider waveeUiSpotifyProvider)
    {
        _rootProvider = waveeUiSpotifyProvider;

    }

    public IWaveeUIProvider RootProvider => _rootProvider;
    public IWaveeUIAuthenticatedProfile? AuthenticatedProfile => _spotifyProfile;

    public event EventHandler? AuthenticationDone;
    public event EventHandler<WaveeUIAuthenticationModule> AuthenticationRequested;

    public ValueTask Initialize()
    {
        var chainedTaskAndCatchException = _rootProvider.SpotifyClient.AuthenticateButFailImmediatlyIfOAuthRequired()
            .ContinueWith(async x =>
            {
                try
                {
                    var innerTask = x.Result;
                    if (innerTask.IsFaulted)
                    {
                        return Task.Run(async () =>
                        {
                            await _rootProvider.SpotifyClient.Authenticate();
                            _spotifyProfile = new WaveeUISpotifyProfile(_rootProvider);
                            AuthenticationDone?.Invoke(this, EventArgs.Empty);
                        });
                    }
                    else
                    {
                        this.AuthenticationRequested?.Invoke(this, new WaveeUIAuthenticationModule(
                            authenticationTask: innerTask.ContinueWith(y =>
                            {
                                y.Wait();
                                if (y.IsCompletedSuccessfully)
                                {
                                    _spotifyProfile = new WaveeUISpotifyProfile(_rootProvider);
                                    AuthenticationDone?.Invoke(this, EventArgs.Empty);
                                }
                            }),
                            rootProvider: _rootProvider
                        ));
                    }
                }
                catch (Exception ex)
                {

                }

                return Task.CompletedTask;
            }).Unwrap();


        return new ValueTask(chainedTaskAndCatchException);

        // Task.Run(async () =>
        // {
        //     try
        //     {
        //         await _rootProvider.SpotifyClient.AuthenticateButFailImmediatlyIfOAuthRequired();
        //     }
        //     catch (Exception ex)
        //     {
        //         // authenticate with oauth!
        //         await _rootProvider.SpotifyClient.Authenticate();
        //     }
        // });
        // return ValueTask.CompletedTask;
    }

    public ValueTask<string> OpenBrowser(string url, Func<string, bool> shouldreturn)
    {
        var tcs = new TaskCompletionSource<string>();
        AuthenticationRequested?.Invoke(this, new WaveeUIAuthenticationModule(
            oAuthUrl: url,
            oAuthCallback: OAuthCallback,
            rootProvider: _rootProvider
        ));

        ValueTask<bool> OAuthCallback(string arg)
        {
            var isOk = shouldreturn(arg);
            if (isOk)
            {
                tcs.TrySetResult(arg);
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        return new ValueTask<string>(tcs.Task);
    }
}


internal sealed class WaveeUISpotifyProfile : IWaveeUIAuthenticatedProfile
{
    private readonly WaveeUISpotifyProvider _provider;
    private readonly Dictionary<string, Dictionary<WaveeSpotifyLibraryItem, ISpotifyItem>> _libraryCache = new();
    private readonly SemaphoreSlim _librarySemaphore = new SemaphoreSlim(1, 1);

    static WaveeUISpotifyProfile()
    {
        Comparers = new()
        {
            [KnownLibraryComponentFilterType.Alphabetical] = x => x.Value.Name,
            [KnownLibraryComponentFilterType.DateAdded] = x => x.Key.AddedAt
        };
    }

    public WaveeUISpotifyProfile(WaveeUISpotifyProvider provider)
    {
        _provider = provider;
        _provider.SpotifyClient.Remote.StateChanged += RemoteOnStateChanged;
        _provider.SpotifyClient.Remote.StatusChanged += RemoteOnStatusChanged;
    }

    private void RemoteOnStatusChanged(object? sender, RemoteConnectionStatusType e)
    {
        switch (e)
        {
            case RemoteConnectionStatusType.NotConnectedDueToError:
                PlaybackStateChanged?.Invoke(this, MapToUIState(_provider.SpotifyClient.Remote.LastError ?? new Exception("Unknown error")));
                break;
            case RemoteConnectionStatusType.Connecting:
                //TODO
                break;
            case RemoteConnectionStatusType.Connected:
                //TODO
                break;
            case RemoteConnectionStatusType.NotConnected:
                //TODO
                break;
        }
    }

    private void RemoteOnStateChanged(object? sender, Option<SpotifyRemoteState> e)
    {
        if (e.IsSome)
        {
            PlaybackStateChanged?.Invoke(this, MapToUIState(e.ValueUnsafe()));
        }
    }

    public IWaveeUIProvider Provider => _provider;
    public event EventHandler<WaveeUIPlaybackState>? PlaybackStateChanged;

    public ValueTask<WaveeUIPlaybackState> ConnectToRemoteStateIfApplicable()
    {
        var remote = _provider.SpotifyClient.Remote;
        switch (remote.Status)
        {
            case RemoteConnectionStatusType.Connected when remote.State.IsSome:
                return new ValueTask<WaveeUIPlaybackState>(MapToUIState(remote.State.ValueUnsafe()));
            case RemoteConnectionStatusType.NotConnectedDueToError:
                {
                    var err = remote.LastError ?? new Exception("Unknown error");
                    return new ValueTask<WaveeUIPlaybackState>(MapToUIState(err));
                }
            default:
                return new ValueTask<WaveeUIPlaybackState>(MapToUIState(new Exception("Unknown error")));
        }
    }

    private WaveeUIPlaybackState MapToUIState(Exception? remoteLastError)
    {
        throw new NotImplementedException();
    }

    private WaveeUIPlaybackState MapToUIState(SpotifyRemoteState x)
    {
        if (x.Item.IsSome)
        {
            var val = x.Item.ValueUnsafe();
            return new WaveeUIPlaybackState(val.Item, x.IsShuffling, x.RepeatState, x.IsPaused, MutateTo(x.Devices, x.ActiveDeviceId), MutateTo(x.Restrictions))
            {
                PositionOffset = x.PositionOffset,
                PositionSw = x.PositionStopwatch
            };
        }

        return new WaveeUIPlaybackState(null, false, WaveeRepeatStateType.None, true,
            LanguageExt.Seq<WaveeUIRemoteDevice>.Empty, LanguageExt.Seq<WaveePlaybackRestrictionType>.Empty)
        {
            PositionOffset = x.PositionOffset,
            PositionSw = x.PositionStopwatch
        };
    }

    private Seq<WaveePlaybackRestrictionType> MutateTo(HashMap<SpotifyRestrictionAppliesForType, Seq<SpotifyKnownRestrictionType>> xRestrictions)
    {
        var output = new List<WaveePlaybackRestrictionType>();
        foreach (var restriction in xRestrictions)
        {
            if (restriction.Value.IsEmpty) continue;

            switch (restriction.Key)
            {
                case SpotifyRestrictionAppliesForType.Shuffle:
                    output.Add(WaveePlaybackRestrictionType.CannotShuffle);
                    break;
                case SpotifyRestrictionAppliesForType.SkippingNext:
                    output.Add(WaveePlaybackRestrictionType.SkipNext);
                    break;
                case SpotifyRestrictionAppliesForType.SkippingPrevious:
                    output.Add(WaveePlaybackRestrictionType.SkipPrevious);
                    break;
                case SpotifyRestrictionAppliesForType.RepeatContext:
                    output.Add(WaveePlaybackRestrictionType.CannotRepeatContext);
                    break;
                case SpotifyRestrictionAppliesForType.RepeatTrack:
                    output.Add(WaveePlaybackRestrictionType.CannotRepeatTrack);
                    break;
                case SpotifyRestrictionAppliesForType.Pausing:
                    break;
                case SpotifyRestrictionAppliesForType.Resuming:
                    break;
                case SpotifyRestrictionAppliesForType.Seeking:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return output.ToSeq();
    }

    private Seq<WaveeUIRemoteDevice> MutateTo(Seq<DeviceInfo> spDevices, Option<string> spActiveDeviceId)
    {
        string? activeDeviceId = null;
        if (spActiveDeviceId.IsSome) activeDeviceId = spActiveDeviceId.ValueUnsafe();

        var output = new WaveeUIRemoteDevice[spDevices.Count];
        for (int i = 0; i < spDevices.Count; i++)
        {
            var spDevice = spDevices.At(i).ValueUnsafe();
            Option<float> volume = Option<float>.None;
            bool isActive = false;
            if (!spDevice.Capabilities.DisableVolume)
            {
                volume = (float)spDevice.Volume / WaveeSpotifyRemoteClient.MAX_VOLUME;
            }

            if (spDevice.DeviceId == activeDeviceId)
            {
                isActive = true;
            }

            var remoteDevice = new WaveeUIRemoteDevice(Id: spDevice.DeviceId, Type: spDevice.DeviceType, Name: spDevice.Name, Volume: volume, IsActive: isActive);
            output[i] = remoteDevice;
        }

        return output.ToSeq();
    }

    public async Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryArtists(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter, CancellationToken cancellation)
    {
        try
        {
            await _librarySemaphore.WaitAsync(cancellation);
            //var items = await SpotifyLibrary.GetArtists(_provider.SpotifyClient);
            if (!_libraryCache.TryGetValue("artist", out var items))
            {
                items = await SpotifyLibrary.GetArtists(_provider.SpotifyClient);
                _libraryCache["artist"] = items;
            }

            return Get(items, sort, sortAscending, filter, cancellation);
        }
        finally
        {
            _librarySemaphore.Release();
        }
    }

    public static Dictionary<KnownLibraryComponentFilterType, Func<KeyValuePair<WaveeSpotifyLibraryItem, ISpotifyItem>, object>> Comparers;

    public async Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryAlbums(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter,
        CancellationToken cancellation)
    {
        try
        {
            await _librarySemaphore.WaitAsync(cancellation);
            if (!_libraryCache.TryGetValue("album;track", out var items))
            {
                items = await SpotifyLibrary.GetAlbumAndTracks(_provider.SpotifyClient);
                _libraryCache["album;track"] = items;
            }

            var albums = items.Where(x => x.Value is SpotifySimpleAlbum);
            return Get(albums, sort, sortAscending, filter, cancellation);
        }
        finally
        {
            _librarySemaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryTracks(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter,
        CancellationToken cancellation)
    {
        try
        {
            await _librarySemaphore.WaitAsync(cancellation);
            if (!_libraryCache.TryGetValue("album;track", out var items))
            {
                items = await SpotifyLibrary.GetAlbumAndTracks(_provider.SpotifyClient);
                _libraryCache["album;track"] = items;
            }

            var tracks = items.Where(x => x.Value is SpotifySimpleTrack);
            return Get(tracks, sort, sortAscending, filter, cancellation);
        }
        finally
        {
            _librarySemaphore.Release();
        }
    }

    public Task<IReadOnlyCollection<LyricsLine>> GetLyricsFor(string id) =>
        _provider.SpotifyClient.Metadata.GetLyricsFor(SpotifyId.FromUri(id));


    public async Task<(string Dark, string Light)> ExtractColorFor(string url)
    {
        var colors = await _provider.SpotifyClient.Metadata.FetchExtractedColors(Seq1(url));
        var color = colors[url];
        return color;
    }

    public Task<bool> ResumeRemoteDevice(bool waitForResponse) =>
        _provider.SpotifyClient.Remote.Resume(waitForResponse);

    public Task<bool> PauseRemoteDevice(bool waitForResponse) =>
        _provider.SpotifyClient.Remote.Pause(waitForResponse);

    public Task<bool> SkipPrevious(bool waitForResponse)
    {
        Option<TimeSpan> seekIfMoreThan = TimeSpan.FromSeconds(2);
        if (seekIfMoreThan.IsSome)
        {
            var position = _provider.SpotifyClient.Remote.State.Map(x => x.Position);
            if (position.IsSome)
            {
                var currPos = position.ValueUnsafe();
                if (currPos > seekIfMoreThan.ValueUnsafe())
                {
                    return SeekTo(TimeSpan.Zero, waitForResponse);
                }
            }
        }

        return _provider.SpotifyClient.Remote.SkipPrev(waitForResponse);
    }

    public Task<bool> SkipNext(bool waitForResponse) =>
        _provider.SpotifyClient.Remote.SkipNext(waitForResponse);
    public Task<bool> SeekTo(TimeSpan position, bool waitForResponse) =>
        _provider.SpotifyClient.Remote.SeekTo(position, waitForResponse);

    public Task<bool> SetShuffle(bool isShuffling, bool waitForResponse) =>
        _provider.SpotifyClient.Remote.SetShuffle(isShuffling, waitForResponse);

    public Task<bool> GoToRepeatState(WaveeRepeatStateType repeatState, bool waitForResponse)
        => _provider.SpotifyClient.Remote.GoToRepeatState(repeatState, waitForResponse);

    public Task<bool> SetVolume(double oneToZero, bool waitForResponse)
        => _provider.SpotifyClient.Remote.SetVolume(oneToZero, waitForResponse);

    public async Task<WaveeAlbumViewModel> GetAlbum(string albumId, ICommand playCommand)
    {
        var spotifyAlbum = await _provider.SpotifyClient.Metadata.GetAlbum(SpotifyId.FromUri(albumId));
        return new WaveeAlbumViewModel(albumId, spotifyAlbum.Name, (uint)spotifyAlbum.Year, spotifyAlbum.Tracks, spotifyAlbum.Images.Head.Url, playCommand);
    }


    private IReadOnlyCollection<LibraryItemViewModel> Get(
        IEnumerable<KeyValuePair<WaveeSpotifyLibraryItem, ISpotifyItem>> items,
        KnownLibraryComponentFilterType sort, bool sortAscending, string? filter, CancellationToken cancellation)
    {
        if (sortAscending)
        {
            items = items.OrderBy(Comparers[sort]);
        }
        else
        {
            items = items.OrderByDescending(Comparers[sort]);
        }

        return items
            .Select(x => new LibraryItemViewModel(x.Value, x.Key.AddedAt, this))
            .ToImmutableArray();
    }
}