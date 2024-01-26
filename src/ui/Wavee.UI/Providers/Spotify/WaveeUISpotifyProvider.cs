using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Spfy;
using Wavee.Spfy.Items;
using Wavee.UI.ViewModels.Library;

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
    }

    public IWaveeUIProvider Provider => _provider;

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
            .Select(static x => new LibraryItemViewModel(x.Value, x.Key.AddedAt))
            .ToImmutableArray();
    }
}