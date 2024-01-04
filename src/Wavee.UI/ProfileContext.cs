using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Tango.Types;
using Wavee.Spotify;
using Wavee.UI.Navigation;
using Wavee.UI.Query.Contracts;
using Wavee.UI.ViewModels.Profile;

namespace Wavee.UI;

/// <summary>
/// /A profile context is a type of context that can be used to invoke commands and queries.
///
/// You should pass this context to your Query and Command objects.
/// </summary>
public abstract class ProfileContext
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;

    protected ProfileContext(IServiceCollection serviceCollection)
    {
        serviceCollection.AddMediator(x => { x.ServiceLifetime = ServiceLifetime.Transient; });

        serviceCollection.AddTransient<INavigationContextFactory, NavigationContextFactory>();
        serviceCollection.AddSingleton(this);

        var sp = serviceCollection.BuildServiceProvider();
        _mediator = sp.GetRequiredService<IMediator>();
        _serviceProvider = sp;
    }

    public static ProfileContext Spotify(IWaveeSpotifyClient client)
    {
        return new SpotifyProfileContext(client);
    }

    public ValueTask<TResponse> Query<TResponse>(IAuthenticatedQuery<TResponse> query)
    {
        query.Profile = this;
        return _mediator.Send(query);
    }
}

internal sealed class SpotifyProfileContext : ProfileContext
{
    public SpotifyProfileContext(IWaveeSpotifyClient client) : base(BuildSp(client))
    {
        Client = client;
    }

    public IWaveeSpotifyClient Client { get; }

    private static IServiceCollection BuildSp(IWaveeSpotifyClient client)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(client);
        return sc;
    }
}