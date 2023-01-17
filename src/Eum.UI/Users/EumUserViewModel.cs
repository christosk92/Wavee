using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Users;
using Eum.Logging;
using Eum.Spotify.playlist4;
using Eum.UI.Items;
using Eum.UI.JsonConverters;
using Eum.UI.Playlists;
using Eum.UI.Services.Playlists;
using Eum.UI.ViewModels.Playlists;
using Refit;
using System.Text.Json;
using Eum.UI.Services;
using Eum.UI.Services.Library;
using Eum.Users;

namespace Eum.UI.Users
{
    [INotifyPropertyChanged]
    public abstract partial class EumUserViewModel
    {
        [ObservableProperty]
        private EumUser _user;
        [ObservableProperty]
        private bool _signedIn;

        protected EumUserViewModel(EumUser user)
        {
            User = user;
        }

        public event EventHandler<ItemId> PlaylistAdded;

        public static EumUserViewModel Create(EumUser user)
        {
            switch (user.Id.Service)
            {
                case ServiceType.Local:
                    throw new NotImplementedException();
                case ServiceType.Spotify:
                    return new EumSpotifyUser(user);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public abstract Task LoginAsync(CancellationToken ct = default);

        public abstract ValueTask<PlaylistViewModel> CreatePlaylist(
            string? title,
            string? image,
            ServiceType[] services);

        public abstract Task Sync();
    }
    public sealed partial class EumSpotifyUser : EumUserViewModel
    {
        private SpotifyPrivateUser? _privateUser;
        private AuthenticatedSpotifyUser? _authenticatedSpotifyUser;
        public EumSpotifyUser(EumUser user) : base(user)
        {
            // LoginCommand = new AsyncRelayCommand(() =>
            // {
            //     return Task.CompletedTask;
            // });
        }


        public SpotifyPrivateUser PrivateUser =>
            _privateUser ??=
                (User.Metadata["privateUser"] is JsonElement jsonElement ? jsonElement : default).Deserialize<SpotifyPrivateUser>(
                    SystemTextJsonSerializationOptions.Default);

        public AuthenticatedSpotifyUser AuthenticatedUser =>
            _authenticatedSpotifyUser ??=
                (User.Metadata["authenticatedUser"] is JsonElement jsonElement ? jsonElement : default).Deserialize<AuthenticatedSpotifyUser>(
                    SystemTextJsonSerializationOptions.Default);

        public override async Task LoginAsync(CancellationToken ct = default)
        {
            var client = Ioc.Default.GetRequiredService<ISpotifyClient>();
            var spotifyClient = await client
                .AuthenticateAsync(new ReusableAuthenticator(AuthenticatedUser.ReusableAuthCredentialsBase64,
                    AuthenticatedUser.ResuableCredentialsType, AuthenticatedUser.Username));
            _privateUser = client.PrivateUser;
            _authenticatedSpotifyUser = spotifyClient;
            User.ProfileName = _privateUser.Name;
            User.ProfilePicture = _privateUser.Avatar?.FirstOrDefault()?.Url;
            User.ReplaceMetadata("authenticatedUser", spotifyClient);
            User.ReplaceMetadata("privateUser", client.PrivateUser);
            User.ThemeService = Ioc.Default.GetRequiredService<IThemeSelectorServiceFactory>()
                .GetThemeSelectorService(User);
            User.LibraryProvider = Ioc.Default.GetRequiredService<ILibraryProviderFactory>()
                .GetLibraryProvider(User);
            _ = Task.Run(() => User.LibraryProvider.InitializeLibrary(ct));
        }


        public override async ValueTask<PlaylistViewModel> CreatePlaylist(
            string? title,
            string? image,
            ServiceType[] services)
        {
            var mainService = User.Id.Service;

            var linkWith = new Dictionary<ServiceType, ItemId>();
            foreach (var serviceType in services
                         .Where(a => a!= mainService))
            {
                //TODO: Create the playlist in each service
            }

            var playlistclients = Ioc.Default.GetRequiredService<ISpotifyClient>();
            var spotifyPlaylist = await playlistclients
                .OpenApiPlaylists.CreatePlaylist(User.Id.Id, new CreatePlaylistRequest
                {
                    Public = true,
                    Name = title,
                });

            var bytes = File.ReadAllBytes(image);
            var file = Convert.ToBase64String(bytes);
            try
            {
                await playlistclients.OpenApiPlaylists.UploadImage(spotifyPlaylist.Id, file);
            }
            catch (ApiException ex)
            {
                S_Log.Instance.LogError(ex);
            }

            var playlistmanager = Ioc.Default.GetRequiredService<IEumPlaylistManager>();

            //main service = UserId,
            //then we link with "services"
            var playlist =
                await playlistmanager.AddPlaylist(title,
                    spotifyPlaylist.Id,
                    image,
                    mainService, User.Id, linkWith);
            var vm = await Task.Run(async () => await Ioc.Default.GetRequiredService<IEumUserPlaylistViewModelManager>()
                .WaitForVm(playlist));
            return vm;
        }

        public override async Task Sync()
        {
            //fetch playlists
            try
            {
                var playlistmanager = Ioc.Default.GetRequiredService<IEumPlaylistManager>();

                var spotifyClient = Ioc.Default.GetRequiredService<ISpotifyClient>();

                using var rootList = await spotifyClient.SpClientPlaylists
                    .GetPlaylists(User.Id.Id, new GetPlaylistsRequest(), CancellationToken.None);

                var data = SelectedListContent.Parser.ParseFrom(await rootList.Content.ReadAsStreamAsync());


                var existingPlaylists = new Dictionary<string, EumPlaylist>();
                foreach (var playlists in await playlistmanager.GetPlaylists(User.Id, false))
                {
                    if (playlists.Id.Service != ServiceType.Spotify) continue;
                    existingPlaylists[playlists.Id.Uri] = playlists;
                    //update
                }


                for (var index = 0; index < data.Contents.Items.Count; index++)
                {
                    var playlist = data.Contents.Items[index];
                    var metaData = data.Contents.MetaItems[index];

                    if (!existingPlaylists.TryGetValue(playlist.Uri, out var eumPlaylist))
                    {
                        eumPlaylist = new EumPlaylist
                        {
                            Id = new ItemId(playlist.Uri),
                            LinkedTo = new Dictionary<ServiceType, ItemId>(),
                            Name = metaData.Attributes.Name,
                            Description = metaData.Attributes.Description,
                            Metadata = metaData.Attributes.FormatAttributes
                                .ToDictionary(a => a.Key, a => a.Value),
                            Tracks = new ItemId[metaData.Length],
                            User = new ItemId($"spotify:user:{metaData.OwnerUsername}"),
                            AlsoUnder = metaData.OwnerUsername == User.Id.Id
                                ? Array.Empty<ItemId>()
                                : new ItemId[]
                                {
                                    User.Id
                                },
                            //reverse index

                            Order = data.Contents.Items.Count - index - 1
                        };
                    }
                    else
                    {
                        eumPlaylist.Name = metaData.Attributes.Name;
                        eumPlaylist.Description  = metaData.Attributes.Description;
                        eumPlaylist.Metadata = metaData.Attributes.FormatAttributes
                            .ToDictionary(a => a.Key, a => a.Value);
                        eumPlaylist.Order = data.Contents.Items.Count - index - 1;
                    }

                    string url = default;
                    if (metaData.Attributes.HasPicture)
                    {
                        //https://i.scdn.co/image/ab67706c0000da8474ca565b7ad0c70e6c2973ce
                        var hex = metaData.Attributes.Picture.BytesToHex();
                        url = $"https://i.scdn.co/image/{hex.ToLower()}";
                    }
                    else if (metaData.Attributes.PictureSize.Any())
                    {
                        url =
                            (metaData.Attributes.PictureSize.FirstOrDefault(a => a.TargetName == "large")
                             ?? metaData.Attributes.PictureSize.First()).Url;
                    }
                    else
                    {
                        //try fetch from spotify api
                        //https://api.spotify.com/v1/playlists/{playlist_id}/images
                        var images = await 
                            spotifyClient.OpenApiPlaylists.GetImages(eumPlaylist.Id.Id, CancellationToken.None);
                        if (images.Length > 0)
                        {
                            url = images.MaxBy(a => a.Height ?? 0).Url;
                        }
                    }
                    eumPlaylist.ImagePath = url;
                    playlistmanager.AddPlaylist(eumPlaylist);
                }

            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
            }
        }
    }
}
