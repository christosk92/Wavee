using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Websocket;
using Eum.UI.Spotify.ViewModels.Users;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Users;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Eum.Connections.Spotify.Cache;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Eum.Connections.Spotify.Attributes;
using Eum.Connections.Spotify.DelegatingHandlers;
using Eum.Connections.Spotify.JsonConverters;
using System.Reflection;
using Eum.Connections.Spotify.Playback;
using Eum.Connections.Spotify.Playback.Player;
using Eum.UI.JsonConverters;
using Eum.UI.Services.Directories;
using Eum.UI.Spotify.ViewModels.Playback;
using Eum.UI.ViewModels.Playback;
using Eum.Logging;
using System.Runtime.InteropServices.ComTypes;
using Eum.UI.Helpers;
using Eum.UI.Services.Home;

namespace Eum.UI.Spotify
{
    public static class ServiceCollectionExt
    {
        public static IServiceCollection AddSpotify(this IServiceCollection services,
            SpotifyConfig config,
            IAudioPlayer withPlayer)
        {
            services.AddTransient<ISignInToXViewModel, SignInToSpotifyViewModel>();
            services.AddTransient<SelectProfileViewModel>();

            services.AddSingleton<PersonalizedRecommendationsProvider>();
            services.AddScoped<LoginViewModel>();
            services.AddSingleton(config);
            services.AddSingleton<ISpotifyConnectionProvider, SpotifyConnectionProvider>();
            services.AddSingleton<IBearerClient, MercuryBearerClient>();
            services.AddTransient<PlaybackViewModel, SpotifyPlaybackViewModel>();
            services.AddSingleton(provider =>
            {
                var bearerClient = provider.GetRequiredService<IBearerClient>();

                return BuildLoggableClient<IOpenTracksClient>(bearerClient);
            });
            services.AddTransient<IMercurySearchClient, MercurySearchClient>();

            services.AddSingleton(provider =>
            {
                var bearerClient = provider.GetRequiredService<IBearerClient>();

                return BuildLoggableClient<IOpenPlaylistsClient>(bearerClient);
            });

            services.AddSingleton(provider =>
            {
                var bearerClient = provider.GetRequiredService<IBearerClient>();

                return BuildLoggableClient<ISpClientPlaylists>(bearerClient);
            });
            services.AddSingleton(provider =>
            {
                var bearerClient = provider.GetRequiredService<IBearerClient>();

                return BuildLoggableClient<IColorLyrics>(bearerClient);
            });

            services.AddTransient<IMercuryTracksClient, MercuryTracksClient>();
            services.AddSingleton(provider =>
            {
                var bearerClient = provider.GetRequiredService<IBearerClient>();
                return BuildLoggableClient<ISpotifyUsersClient>(bearerClient);
            });

            services.AddSingleton(provider =>
            {
                var v = provider.GetRequiredService<IBearerClient>();
                return BuildLoggableClient<IViewsClient>(v);
            });
            services.AddTransient<IExtractedColorsClient, ExtractedColorClient>();
            services.AddTransient<IArtistClient, ArtistsClientWrapper>()
                .AddTransient<IMercuryArtistClient, MercuryArtistClient>()
                .AddSingleton(provider =>
                {
                    var bearerClient = provider.GetRequiredService<IBearerClient>();

                    return BuildLoggableClient<IOpenArtistClient>(bearerClient);
                });

            services.AddTransient<IAlbumsClient, AlbumsCLientWrapper>()
                .AddTransient<IMercuryAlbumsClient, MercuryAlbumClient>();
                // .AddSingleton(provider =>
                // {
                //     var bearerClient = provider.GetRequiredService<IBearerClient>();
                //
                //     return BuildLoggableClient<IOpenArtistClient>(bearerClient);
                // });

            services.AddTransient<ITracksClient, TracksClientWrapper>();
            services.AddSingleton<IEventService, EventService>();

            services.AddSingleton<ITimeProvider, TimeProvider>();
            services.AddSingleton<ISpotifyConnectClient, SpotifyConnectClient>();
            services.AddSingleton<ISpotifyWebsocket, SpotifyWebSocket>();
            services.AddSingleton<IMercuryClient, MercuryClient>();
            services.AddSingleton<IAudioKeyManager, AudioKeyManager>();

            services.AddSingleton<ISpotifyClient, SpotifyClient>();
            services.AddSingleton<IAudioPlayer>(withPlayer);

            if (config.CachePath != null)
                services.AddSingleton<ICacheManager>(new JournalCacheManager(config.CachePath));
            services.AddSingleton<ISpotifyPlaybackClient, SpotifyPlaybackClient>();

            if (!string.IsNullOrEmpty(config.LogPath))
            {
                S_Log.Instance.InitializeDefaults(config.LogPath, null);
            }
            return services;
        }

        private static T BuildLoggableClient<T>(IBearerClient bearerClient)
        {
            var type = typeof(T);
            var baseUrl = ResolveBaseUrlFromAttribute(type);

            var handler = new LoggingHandler(new HttpClientHandler(), bearerClient);

            var client =
                new HttpClient(handler)
                {
                    BaseAddress = new Uri(baseUrl)
                };

            var refitSettings = new RefitSettings(DefaultOptions.RefitSettings);
            var refitClient = RestService.For<T>(client, refitSettings);

            return refitClient;
        }


        private static string ResolveBaseUrlFromAttribute(MemberInfo type)
        {
            var attribute = Attribute.GetCustomAttributes(type);

            if (attribute.FirstOrDefault(x => x is BaseUrlAttribute) is BaseUrlAttribute baseUrlAttribute)
                return baseUrlAttribute.BaseUrl;

            // if (attribute.Any(x => x is ResolvedDealerEndpoint)) return await ApResolver.GetClosestDealerAsync();
            //
            // if (attribute.Any(x => x is ResolvedSpClientEndpoint)) return await ApResolver.GetClosestSpClient();

            if (attribute.Any(x => x is OpenUrlEndpoint)) return "https://api.spotify.com/v1";
            //TODO: ApResolver
            //gae2-spclient.spotify.com:443
            if (attribute.Any(x => x is SpClientEndpoint)) return "https://gae2-spclient.spotify.com/";

            throw new InvalidDataException("No BaseUrl or ResolvedEndpoint attribute was defined");
        }
    }
}