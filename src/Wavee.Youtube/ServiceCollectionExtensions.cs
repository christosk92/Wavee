using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Wavee.Youtube;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddYoutube(this IServiceCollection services)
    {
        services.AddSingleton<IWaveeYoutubeClient, WaveeYoutubeClient>();

        services.AddYoutubeClient();

        return services;
    }


    private static void AddYoutubeClient(this IServiceCollection services)
    {
        services.AddHttpClient(Constants.YoutubeClientName);
    }
}