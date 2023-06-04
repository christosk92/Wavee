using Microsoft.UI.Xaml.Controls;
using System;
using Wavee.UI.Enums;
using Wavee.UI.Models.Common;
using Wavee.UI.WinUI.Views.Home;
using Wavee.UI.ViewModels;
using FontFamily = Microsoft.UI.Xaml.Media.FontFamily;
using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public ShellView(SpotifyUser spotifyUser)
        {
            this.InitializeComponent();
            var songsLibraryItem = BuildLibrary(LibraryItemType.Songs);
            var albumsLibraryItem = BuildLibrary(LibraryItemType.Albums);
            var artistsLibraryItem = BuildLibrary(LibraryItemType.Artists);
            var podcastsLibraryItem = BuildLibrary(LibraryItemType.Podcasts);

            SidebarControl.UserSettings = State.Instance.Settings;
            SidebarControl.UserInfo = spotifyUser;

            SidebarControl.FixedItems = new[]
            {
                new SidebarItem { Title = "For You", IsAHeader = true },
                BuildHome(),
                BuildFeed(),

                new SidebarItem { Title = "Your Library",IsAHeader = true },
                songsLibraryItem,
                albumsLibraryItem,
                artistsLibraryItem,
                podcastsLibraryItem,
            };

            void OnLibraryItemAdded(Seq<AudioId> id)
            {
                //inc count
                var tracksAdded = id.Where(c => c.Type is AudioItemType.Track).Count;
                var albumsAdded = id.Where(c => c.Type is AudioItemType.Album).Count;
                var artistsAdded = id.Where(c => c.Type is AudioItemType.Artist).Count;
                var podcastsAdded = id.Where(c => c.Type is AudioItemType.PodcastEpisode).Count;

                songsLibraryItem.Count += tracksAdded;
                albumsLibraryItem.Count += albumsAdded;
                artistsLibraryItem.Count += artistsAdded;
                podcastsLibraryItem.Count += podcastsAdded;
            }
            void OnLibraryItemRemoved(Seq<AudioId> id)
            {
                //dec count
                var tracksRemoved = id.Where(c => c.Type is AudioItemType.Track).Count;
                var albumsRemoved = id.Where(c => c.Type is AudioItemType.Album).Count;
                var artistsRemoved = id.Where(c => c.Type is AudioItemType.Artist).Count;
                var podcastsRemoved = id.Where(c => c.Type is AudioItemType.PodcastEpisode).Count;

                songsLibraryItem.Count -= tracksRemoved;
                albumsLibraryItem.Count -= albumsRemoved;
                artistsLibraryItem.Count -= artistsRemoved;
                podcastsLibraryItem.Count -= podcastsRemoved;
            }

            ViewModel = new ShellViewModel(OnLibraryItemAdded, OnLibraryItemRemoved);
            GC.Collect();
        }
        public ShellViewModel ViewModel { get; }

        private void NavigateTo(Type type, object? parameter)
        {

        }
        private SidebarItem BuildLibrary(LibraryItemType type)
        {
            return new SidebarItem
            {
                Title = type.ToString(),
                Icon = type switch
                {
                    LibraryItemType.Songs => new FontIcon
                    {
                        Glyph = "\uE00B",
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    },
                    LibraryItemType.Albums => new FontIcon
                    {
                        Glyph = "\uE93C",
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    },
                    LibraryItemType.Artists => new FontIcon
                    {
                        Glyph = "\uEBDA",
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    },
                    LibraryItemType.Podcasts => new FontIcon
                    {
                        Glyph = "\uEB44",
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                },
                Count = 0,
                IsCountable = true,
                IsEnabled = type is not LibraryItemType.Podcasts,
                Navigation = null //TODO,
            };
        }

        private SidebarItem BuildFeed()
        {
            return new SidebarItem
            {
                Title = "Browse",
                Icon = new FontIcon
                {
                    Glyph = "\uE794",
                    FontFamily = new FontFamily("/Assets/Fonts/MediaPlayerIcons.ttf#Media Player Fluent Icons"),
                },
                IsAHeader = false,
                Navigation = null //TODO
            };
        }

        private SidebarItem BuildHome()
        {
            return new SidebarItem
            {
                Title = "Home",
                Icon = new SymbolIcon(Symbol.Home),
                IsAHeader = false,
                Navigation = () => NavigateTo(typeof(HomeView), null)
            };
        }
    }
}
