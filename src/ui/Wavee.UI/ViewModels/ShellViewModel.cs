using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using DynamicData.Binding;
using Wavee.UI.Models;
using System.Reactive.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.Models.Response;
using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.ViewModels;

public sealed class ShellViewModel<R> : ReactiveObject where R : struct, HasFile<R>, HasDirectory<R>, HasLocalPath<R>, HasSpotify<R>
{
    private readonly R _runtime;
    private User _user;

    public ShellViewModel(R runtime, User user,
        Action<Seq<AudioId>> onLibraryItemAdded,
        Action<Seq<AudioId>> onLibraryItemRemoved)
    {
        Playback = new PlaybackViewModel<R>(runtime);
        PlaylistsVm = new PlaylistsViewModel<R>(runtime);
        Library = new LibraryViewModel<R>(runtime, onLibraryItemAdded, onLibraryItemRemoved, user.Id);
        User = user;

        _runtime = runtime;
        Instance = this;

        Task.Run(async () =>
        {
            var userAff = (await Spotify<R>
                .GetFromPublicApi<PrivateSpotifyUser>("/me", CancellationToken.None)
                .Run(runtime));
            var user = userAff.ThrowIfFail();
            var image = user.Images
                .HeadOrNone().Map(x => x.Url);

            _ = RxApp.MainThreadScheduler.Schedule(() =>
            {
                User = new User(
                    Id: User.Id,
                    IsDefault: User.IsDefault,
                    ImageId: image,
                    DisplayName: user.DisplayName,
                    Metadata: User.Metadata
                );
            });
        });
    }

    public User User
    {
        get => _user;
        set => this.RaiseAndSetIfChanged(ref _user, value);
    }
    public static ShellViewModel<R> Instance { get; private set; }
    public PlaylistsViewModel<R> PlaylistsVm { get; private set; }
    public PlaybackViewModel<R> Playback { get; private set; }
    public LibraryViewModel<R> Library { get; private set; }
}