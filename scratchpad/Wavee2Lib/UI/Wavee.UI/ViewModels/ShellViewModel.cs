using System.Reactive.Concurrency;
using LanguageExt;
using LanguageExt.Pipes;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.Models.Common;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Playlists;

namespace Wavee.UI.ViewModels;

public sealed class ShellViewModel : ReactiveObject
{
    public ShellViewModel(
        Action<Seq<AudioId>> onLibraryItemAdded,
        Action<Seq<AudioId>> onLibraryItemRemoved,
        SpotifyUser user)
    {
        Playlists = new PlaylistsViewModel();
        Library = new LibrariesViewModel(onLibraryItemAdded, onLibraryItemRemoved, user.Id);
        Player = new PlaybackViewModel();
        State.Instance.User = user;
        Task.Run(async () =>
        {
            var newUser = await State.Instance.Client.PublicApi.GetMe();
            var image = newUser.Images
                .HeadOrNone().Map(x => x.Url);

            _ = RxApp.MainThreadScheduler.Schedule(() =>
            {
                State.Instance.User = new SpotifyUser(
                    Id: user.Id,
                    DisplayName: newUser.DisplayName,
                    ImageUrl: image
                );
            });
        });

    }
    public State State => State.Instance;

    public PlaylistsViewModel Playlists { get; }
    public LibrariesViewModel Library { get; }
    public PlaybackViewModel Player { get; }
}