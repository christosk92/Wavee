using Microsoft.Extensions.DependencyInjection;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Playback.PlayerHandlers;
using Wavee.UI.Services.Db;
using Wavee.UI.Services.Import;
using Wavee.UI.ViewModels.Home;

namespace Wavee.UI.Helpers.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection AddLocal(this IServiceCollection services)
        {
            services.AddScoped<ILocalAudioDb, LocalAudioDb>();

            services.AddSingleton<ImportService>();

            services.AddTransient<LocalHomeViewModel>();

            //services.AddTransient<IPlaycountService, PlaycountService>();
            services.AddTransient<LocalPlayerHandler>();

            return services;
        }
    }
}
