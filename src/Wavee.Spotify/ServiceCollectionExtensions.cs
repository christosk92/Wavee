using LiteDB;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.Spotify.Application.Remote;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Infrastructure.MessageHandlers;
using Wavee.Spotify.Infrastructure.Persistent;

namespace Wavee.Spotify;

public static class ServiceCollectionExtensions
{
    public static IncompleteSpotifyBuilder AddSpotify(this IServiceCollection services,
        SpotifyClientConfig spotifyClientConfig)
    {
        services.AddSingleton(spotifyClientConfig);
        services.AddTransient<SpotifyTokenMessageHandler>();

        services.AddSpotifyPublicApiHttpClient();
        services.AddSpotifyPartnerApiHttpClient();
        services.AddSpotifyPrivateApiHttpClient();

        services.AddSingleton<ISpotifyClient, SpotifyClient>();

        services.AddSingleton<ILiteDatabase>(new LiteDatabase(Path.Combine(spotifyClientConfig.Storage.Path,
            "data.db")));

        services.AddScoped<ISpotifyStoredCredentialsRepository, SpotifyStoredCredentialsRepository>();
        services.AddScoped<ISpotifyAccessTokenRepository, SpotifyAccessTokenRepository>();

        services.AddSingleton<ISpotifyRemoteClient, SpotifyRemoteHolder>();
        
        return new IncompleteSpotifyBuilder(services);
    }


    public static void AddSpotifyPublicApiHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(Constants.SpotifyPublicApiHttpClient, client =>
        {
            client.BaseAddress = new Uri("https://api.spotify.com/v1/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddHttpMessageHandler<SpotifyTokenMessageHandler>();
    }

    public static void AddSpotifyPartnerApiHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(Constants.SpotifyPartnerApiHttpClient, client =>
        {
            client.BaseAddress = new Uri("https://partner.api.spotify.com/v1/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddHttpMessageHandler<SpotifyTokenMessageHandler>();
    }

    public static void AddSpotifyPrivateApiHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(Constants.SpotifyPrivateApiHttpClient, client =>
        {
        }).AddHttpMessageHandler<SpotifyTokenMessageHandler>();
    }
}

public readonly struct IncompleteSpotifyBuilder
{
    private readonly IServiceCollection _services;

    internal IncompleteSpotifyBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public IServiceCollection WithStoredOrOAuthModule(FetchRedirectUrlDelegate openBrowser)
    {
        return _services.AddSingleton<ISpotifyAuthModule>((sp) =>
        {
            return new SpotifyStoredOrOAuthModule(openBrowser, sp);
        });
    }

    public IServiceCollection WithStoredCredentialsModule()
    {
        return _services.AddSingleton<ISpotifyAuthModule>((sp) =>
        {
            return new SpotifyStoredCredentialsModule(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<ISpotifyStoredCredentialsRepository>()
            );
        });
    }

    public IServiceCollection WithOAuthModule(FetchRedirectUrlDelegate openBrowser)
    {
        return _services.AddSingleton<ISpotifyAuthModule>((sp) =>
        {
            return new SpotifyOAuthModule(openBrowser,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<SpotifyClientConfig>(),
                sp.GetRequiredService<ISpotifyStoredCredentialsRepository>()
            );
        });
    }
}