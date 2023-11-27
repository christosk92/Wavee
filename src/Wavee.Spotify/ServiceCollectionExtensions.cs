using LiteDB;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Domain.Playback.Player;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.Spotify.Application.Remote;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Infrastructure.MessageHandlers;
using Wavee.Spotify.Infrastructure.Persistent;

namespace Wavee.Spotify;

public static class ServiceCollectionExtensions
{
    public static AuthMissingSpotifyBuilder AddSpotify(this IServiceCollection services,
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

        services.AddSingleton<SpotifyRemoteHolder>();

        return new AuthMissingSpotifyBuilder(services);
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
        services.AddHttpClient(Constants.SpotifyRemoteStateHttpClietn, client => { })
            .AddHttpMessageHandler<SpotifyTokenMessageHandler>();
    }
}

public class PlayerMissingSpotifyBuilder
{
    private readonly IServiceCollection _services;

    internal PlayerMissingSpotifyBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public IServiceCollection WithPlayer<TPlayer>() where TPlayer : class, IWaveePlayer
    {
        return _services.AddSingleton<IWaveePlayer, TPlayer>();
    }

    public IServiceCollection WithPlayer<TPlayer>(Func<IServiceProvider, TPlayer> factory)
        where TPlayer : class, IWaveePlayer
    {
        return _services.AddSingleton<IWaveePlayer, TPlayer>(factory);
    }

    public IServiceCollection WithPlayer(IWaveePlayer playerInstance)
    {
        return _services.AddSingleton<IWaveePlayer>(playerInstance);
    }
}

public class AuthMissingSpotifyBuilder
{
    private readonly IServiceCollection _services;

    internal AuthMissingSpotifyBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public PlayerMissingSpotifyBuilder WithStoredOrOAuthModule(FetchRedirectUrlDelegate openBrowser)
    {
        return new PlayerMissingSpotifyBuilder(_services.AddSingleton<ISpotifyAuthModule>((sp) =>
        {
            return new SpotifyStoredOrOAuthModule(openBrowser, sp);
        }));
    }

    public PlayerMissingSpotifyBuilder WithStoredCredentialsModule()
    {
        return new PlayerMissingSpotifyBuilder(_services.AddSingleton<ISpotifyAuthModule>((sp) =>
        {
            return new SpotifyStoredCredentialsModule(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<ISpotifyStoredCredentialsRepository>()
            );
        }));
    }

    public PlayerMissingSpotifyBuilder WithOAuthModule(FetchRedirectUrlDelegate openBrowser)
    {
        return new PlayerMissingSpotifyBuilder(_services.AddSingleton<ISpotifyAuthModule>((sp) =>
        {
            return new SpotifyOAuthModule(openBrowser,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<SpotifyClientConfig>(),
                sp.GetRequiredService<ISpotifyStoredCredentialsRepository>()
            );
        }));
    }
}