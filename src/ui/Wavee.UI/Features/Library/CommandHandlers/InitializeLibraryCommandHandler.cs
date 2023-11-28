using System.Collections.Concurrent;
using Mediator;
using Nito.AsyncEx;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Features.Library.Commands;
using Wavee.UI.Features.Library.DataAcces;

namespace Wavee.UI.Features.Library.CommandHandlers;

public sealed class InitializeLibraryCommandHandler : ICommandHandler<InitializeLibraryCommand, TaskCompletionSource>
{
    private static readonly AsyncLock _asyncLock = new();
    private readonly ISpotifyClient _spotifyClient;
    private readonly ILibraryRepository _libraryRepository;
    private static ConcurrentDictionary<ISpotifyClient, TaskCompletionSource> _initializeLibraryTasks = new();
    public InitializeLibraryCommandHandler(ISpotifyClient spotifyClient, ILibraryRepository libraryRepository)
    {
        _spotifyClient = spotifyClient;
        _libraryRepository = libraryRepository;
    }

    public async ValueTask<TaskCompletionSource> Handle(InitializeLibraryCommand command, CancellationToken cancellationToken)
    {
        using (await _asyncLock.LockAsync())
        {
            if (_initializeLibraryTasks.TryGetValue(_spotifyClient, out var tcs))
            {
                return tcs;
            }

            tcs = new TaskCompletionSource();
            _initializeLibraryTasks.TryAdd(_spotifyClient, tcs);
            _ = Task.Run(async () =>
            {
                try
                {
                    await InitializeLibrary(_spotifyClient);
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, cancellationToken);

            return tcs;
        }
    }

    private async Task InitializeLibrary(ISpotifyClient spotifyClient)
    {
        var user = (await spotifyClient.User).CanonicalUsername;
        var alreadyDidInitialFetch = _libraryRepository.ContainsAny(user);
        if (alreadyDidInitialFetch)
        {
            return;
        }

        var artists = await spotifyClient.Library.GetArtists();
    }
}