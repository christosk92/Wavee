using DynamicData;
using ReactiveUI;
using System.Collections.ObjectModel;
using DynamicData.Binding;
using Wavee.UI.Models;
using System.Reactive.Linq;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.ViewModels;

public sealed class ShellViewModel<R> : ReactiveObject where R : struct, HasFile<R>, HasDirectory<R>, HasLocalPath<R>, HasSpotify<R>
{
    private readonly R _runtime;
    public ShellViewModel(R runtime, User user)
    {
        Playback = new PlaybackViewModel<R>(runtime);
        PlaylistsVm = new PlaylistsViewModel<R>(runtime);
        User = user;

        _runtime = runtime;
        Instance = this;

        var observable = Spotify<R>.ObserveRootlist()
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(async c =>
            {
                //Fetch new rootlist.
                //just easier.
                return unit;
            })
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe();
    }
    public User User { get; }
    public static ShellViewModel<R> Instance { get; private set; }
    public PlaylistsViewModel<R> PlaylistsVm { get; private set; }
    public PlaybackViewModel<R> Playback { get; private set; }
}