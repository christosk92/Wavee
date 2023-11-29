using System.Net;
using LiteDB;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Domain.Playback.Player;
using Wavee.Spotify.Application.Album;
using Wavee.Spotify.Application.Artist;
using Wavee.Spotify.Application.AudioKeys;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.Spotify.Application.Library;
using Wavee.Spotify.Application.Remote;
using Wavee.Spotify.Application.StorageResolve;
using Wavee.Spotify.Application.Tracks;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Tracks;
using Wavee.Spotify.Infrastructure.LegacyAuth;
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
        services.AddTransient<SpotifyPrependSpClientUrlHandler>();

        services.AddSpotifyPublicApiHttpClient();
        services.AddSpotifyPartnerApiHttpClient();
        services.AddSpotifyPrivateApiHttpClient();
        services.AddPlaybackHttpClient();

        services.AddSingleton<ISpotifyClient, SpotifyClient>();

        services.AddSingleton<ILiteDatabase>(new LiteDatabase(Path.Combine(spotifyClientConfig.Storage.Path,
            "data.db")));

        services.AddScoped<ISpotifyStoredCredentialsRepository, SpotifyStoredCredentialsRepository>();
        services.AddScoped<ISpotifyAccessTokenRepository, SpotifyAccessTokenRepository>();

        services.AddSingleton<SpotifyRemoteHolder>();
        services.AddSingleton<SpotifyTcpHolder>();


        services.AddScoped<ISpotifyTrackClient, SpotifyTrackClient>();
        services.AddScoped<ISpotifyTrackRepository, SpotifyTrackRepository>();

        services.AddScoped<ISpotifyAudioKeyClient, SpotifyAudioKeyClient>();

        services.AddScoped<ISpotifyStorageResolver, SpotifyStorageResolver>();

        services.AddScoped<ISpotifyLibraryClient, SpotifyLibraryClient>();
        services.AddScoped<ISpotifyArtistClient, SpotifyArtistClient>();
        services.AddScoped<ISpotifyAlbumClient, SpotifyAlbumClient>();
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
            client.BaseAddress = new Uri("https://api-partner.spotify.com/pathfinder/v1/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddHttpMessageHandler<SpotifyTokenMessageHandler>();
    }

    public static void AddPlaybackHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(Constants.SpotifyCdnPlaybackClientName, client =>
        {
            //https://audio4-ak-spotify-com.akamaized.net/audio/f529eee95393647091d45b17dd1cc4630da1a0ae?__token__=exp=1701163282~hmac=03d01eab0da50b9b777bcf2edc46b8be0637ea9ebfa0d4874d519303144a5545
            //force http 3
            client.DefaultRequestVersion = HttpVersion.Version30;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            client.BaseAddress = new Uri("https://audio4-ak-spotify-com.akamaized.net");
        });
    }

    public static void AddSpotifyPrivateApiHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(Constants.SpotifyRemoteStateHttpClietn, client => { })
            .AddHttpMessageHandler<SpotifyTokenMessageHandler>()
            .AddHttpMessageHandler<SpotifyPrependSpClientUrlHandler>();
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
                sp.GetRequiredService<ISpotifyStoredCredentialsRepository>(),
                sp.GetRequiredService<SpotifyTcpHolder>(),
                sp.GetRequiredService<SpotifyClientConfig>()
            );
        }));
    }

    public PlayerMissingSpotifyBuilder WithOAuthModule(FetchRedirectUrlDelegate openBrowser)
    {
        return new PlayerMissingSpotifyBuilder(_services.AddSingleton<ISpotifyAuthModule>((sp) =>
        {
            return new SpotifyOAuthModule(openBrowser,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<SpotifyClientConfig>(),
                sp.GetRequiredService<SpotifyTcpHolder>()
            );
        }));
    }
}