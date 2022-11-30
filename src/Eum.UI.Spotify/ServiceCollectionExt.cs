using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Clients;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Websocket;
using Refit;

namespace Eum.UI.Spotify
{
    public static class ServiceCollectionExt
    {
        public static IServiceCollection AddSpotify(this IServiceCollection services)
        {
            services.AddSingleton<ISpotifyConnectionProvider, SpotifyConnectionProvider>();
            services.AddSingleton<IBearerClient, MercuryBearerClient>();

            services.AddRefitClient<ISpotifyUsersClient>(new RefitSettings());
            services.AddTransient<IArtistClient, ArtistsClientWrapper>();
            services.AddTransient<ITracksClient, TracksClientWrapper>();
            services.AddSingleton<IEventService, EventService>();

            services.AddSingleton<ITimeProvider, TimeProvider>();
            services.AddSingleton<ISpotifyConnectClient, SpotifyConnectClient>();

            services.AddSingleton<IMercuryClient, MercuryClient>();
            services.AddSingleton<IAudioKeyManager, AudioKeyManager>();

            services.AddScoped<ISpotifyClient, SpotifyClient>();
        }
    }
}
