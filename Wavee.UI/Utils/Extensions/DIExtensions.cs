using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wavee.Player.NAudio;
using Wavee.UI.AudioImport;
using Wavee.UI.AudioImport.Database;
using Wavee.UI.Identity;
using Wavee.UI.Identity.Users;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Identity.Users.Directories;
using Wavee.UI.ViewModels.ForYou.Home;
using Wavee.UI.ViewModels.ForYou.Recommended;
using Wavee.UI.ViewModels.Identity;
using Wavee.UI.ViewModels.Playback.Impl;

namespace Wavee.UI.Utils.Extensions
{
    public static class DiExtensions
    {
        public static IServiceCollection AddSpotify(this IServiceCollection serviceCollection,
            string workDir)
        {
            serviceCollection.AddTransient<AbsCredentialsViewModel, SpotifyCredentialsViewModel>();


            serviceCollection
                .AddSingleton(provider =>
                {
                    var loggerfactory = provider.GetService<ILoggerFactory>();

                    return new WaveeUserManager(ServiceType.Spotify,
                        workDir, new UserDirectories(ServiceType.Spotify, workDir),
                        loggerfactory?.CreateLogger<WaveeUserManager>());
                });

            return serviceCollection;
        }

        public static IServiceCollection AddLocal(this IServiceCollection serviceCollection,
            string workDir)
        {
            serviceCollection
                .AddSingleton(provider =>
                {
                    var loggerfactory = provider.GetService<ILoggerFactory>();

                    return new WaveeUserManager(ServiceType.Local,
                        workDir, new UserDirectories(ServiceType.Local, workDir),
                        loggerfactory?.CreateLogger<WaveeUserManager>());
                });
            serviceCollection.AddSingleton<LocalFilePlayer>();
            serviceCollection.AddTransient<LocalHomeViewModel>()
                .AddTransient<LocalRecommendedViewModel>()
                .AddTransient<SeeAllImportedTracksViewModel>();

            serviceCollection.AddSingleton<LocalAudioManagerViewModel>();

            serviceCollection.AddSingleton<ILiteDatabase>(new LiteDatabase(Path.Combine(workDir, "audio.db")));

            serviceCollection.AddSingleton<IAudioDb>(provider =>
            {
                var loggerfactory = provider.GetService<ILoggerFactory>();

                return new LiteDbAudioDb(provider.GetRequiredService<ILiteDatabase>(),
                    loggerfactory?.CreateLogger<LiteDbAudioDb>(),
                    workDir
                );
            });

            serviceCollection.AddTransient<LocalPlayerHandler>();

            return serviceCollection;
        }
    }
}