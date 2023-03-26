using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Wavee.Enums;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Navigation;
using Wavee.UI.Services.Import;
using Wavee.UI.ViewModels.Home;
using Wavee.UI.ViewModels.Libray;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Shell.Sidebar;
using Wavee.UI.ViewModels.User;

namespace Wavee.UI.ViewModels.Shell
{
    public class ShellViewModel
    {
        public UserViewModel User
        {
            get;
        }
        public PlaybackViewModel PlaybackViewModel
        {
            get;
        }
        public ObservableCollection<ISidebarItem> SidebarItems
        {
            get;
        }
        public static ShellViewModel Instance
        {
            get; private set;
        }
        public ShellViewModel(UserViewModel user,
            IStringLocalizer stringLocalizer,
            ILoggerFactory? loggerFactory = null)
        {
            Instance = this;
            PlaybackViewModel = new PlaybackViewModel(user, loggerFactory?.CreateLogger<PlaybackViewModel>())
            {
                ExpandCoverImageCommand = new RelayCommand(() =>
                {
                    if (user.LargeImage)
                    {
                        user.LargeImage = false;
                    }
                    else
                    {
                        user.LargeImage = true;
                        user.SidebarExpanded = true;
                    }
                })
            };
            User = user;
            var baseSidebarItems = new ObservableCollection<ISidebarItem>
            {
                new SidebarItemHeader(stringLocalizer.GetValue("/Shell/ForYou/Header")),
                new GenericSidebarItem(
                    Id: "home",
                    Content: stringLocalizer.GetValue("/Shell/ForYou/Home"),
                    Icon: "\uE10F",
                    NavigateTo: user.ForProfile.ServiceType is ServiceType.Local ? typeof(LocalHomeViewModel) : null),
                new GenericSidebarItem(
                    Id: "recommended",
                    Content: stringLocalizer.GetValue("/Shell/ForYou/Recommended"),
                    Icon:"\uE794",
                    NavigateTo: null),

                new SidebarItemHeader(stringLocalizer.GetValue("/Shell/Library/Header")),

                new CountedSidebarItem(Id: "library.songs",
                    Content: stringLocalizer.GetValue("/Shell/Library/Songs"),
                    Icon:"\uEB52",
                    Count: 0,
                    NavigateTo: typeof(LibraryRootViewModel),
                    NavigateToParameter: new LibraryNavigationParameters(
                        NavigateTo: nameof(LibrarySongsViewModel),
                        Hearted:true,
                        ForService: user.ForProfile.ServiceType)
                    ),
                new CountedSidebarItem(
                    Id:"library.albums",
                    Content : stringLocalizer.GetValue("/Shell/Library/Albums"),
                    Icon:"\uE93C",
                    Count: 0,
                    NavigateTo: typeof(LibraryRootViewModel),
                    NavigateToParameter: new LibraryNavigationParameters(
                        NavigateTo: nameof(LibraryAlbumsViewModel),
                        Hearted:true,
                        ForService: user.ForProfile.ServiceType)
                ),
                new CountedSidebarItem(Id:
                    "library.artists",
                    Content: stringLocalizer.GetValue("/Shell/Library/Artists"),
                    Icon:"\uEBDA",
                    Count:0,
                    NavigateTo: typeof(LibraryRootViewModel),
                    NavigateToParameter: new LibraryNavigationParameters(
                        NavigateTo: nameof(LibraryArtistsViewModel),
                        Hearted:true,
                        ForService: user.ForProfile.ServiceType)
                ),
            };

            if (user.ForProfile.ServiceType is ServiceType.Spotify)
            {
                baseSidebarItems.Add(new CountedSidebarItem(
                    Id: "library.podcasts",
                    Content: stringLocalizer.GetValue("/Shell/Library/Podcasts"),
                    Icon: "\uEB44",
                    Count: 0,
                    NavigateTo: typeof(LibraryRootViewModel),
                    NavigateToParameter: new LibraryNavigationParameters(
                        NavigateTo: nameof(LibrarySongsViewModel),
                        Hearted: true,
                        ForService: user.ForProfile.ServiceType)
                    )
                );
            }
            else if (user.ForProfile.ServiceType is ServiceType.Local)
            {
                //register handlers for import
                var importService = Ioc.Default.GetRequiredService<ImportService>();
                importService.ImportCompleted += ImportServiceOnImportCompleted;
            }

            baseSidebarItems.Add(new CreatePlaylistButtonSidebarItem(
                Content: stringLocalizer.GetValue("/Shell/Playlists/Header"),
                NavigateTo: null));
            SidebarItems = baseSidebarItems;
            _ = UpdateCounts();
        }

        private async void ImportServiceOnImportCompleted(object? sender, EventArgs e)
        {
            //TODO: Unregister from this
            RxApp.MainThreadScheduler.Schedule(async () =>
            {
                await UpdateCounts();
            });
        }

        private async Task UpdateCounts()
        {
            foreach (var sidebarItem in SidebarItems)
            {
                if (sidebarItem is CountedSidebarItem s)
                {
                    switch (s.Id)
                    {
                        case "library.songs":
                            switch (User.ForProfile.ServiceType)
                            {
                                case ServiceType.Local:
                                    s.Count = await Ioc.Default.GetRequiredService<ILocalAudioDb>()
                                        .Count();
                                    break;
                                case ServiceType.Spotify:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case "library.albums":
                            switch (User.ForProfile.ServiceType)
                            {
                                case ServiceType.Local:
                                    const string query = "SELECT COUNT(DISTINCT Album) FROM MediaItems";
                                    s.Count = Ioc.Default.GetRequiredService<ILocalAudioDb>()
                                        .Count(query);
                                    break;
                                case ServiceType.Spotify:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case "library.artists":
                            switch (User.ForProfile.ServiceType)
                            {
                                case ServiceType.Local:
                                    //artists are stored as a string of json arrays, so we need to use a custom query
                                    const string query = "SELECT COUNT(DISTINCT Performers) FROM MediaItems";
                                    s.Count = Ioc.Default.GetRequiredService<ILocalAudioDb>()
                                        .Count(query);
                                    break;
                                case ServiceType.Spotify:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                    }
                }
            }
        }
    }
}
