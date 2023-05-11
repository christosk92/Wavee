using System.Runtime.CompilerServices;
using LanguageExt.Effects.Traits;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Infrastructure.Live;
using Wavee.Spotify.Cache;

[assembly: InternalsVisibleTo("Wavee.Spotify.Tests")]
[assembly: InternalsVisibleTo("Wavee.Spotify")]
[assembly: InternalsVisibleTo("Wavee.Player")]
namespace Wavee;

internal static class WaveeCore
{
    public static Ref<Option<ILogger>> Logger = Ref(Option<ILogger>.None);
    public static Ref<Option<DatabaseIO>> Database = Ref(Option<DatabaseIO>.None);


    static WaveeCore()
    {
        Runtime = WaveeRuntime.New(Logger, Database);

        Logger.OnChange().Subscribe(c =>
        {
            Runtime.Env.Logger = c.IfNone(NullLogger.Instance);
        });
        Database.OnChange().Subscribe(c =>
        {
            Runtime.Env.Database = c.IfNone(new EmptyDb());
        });
    }

    public static WaveeRuntime Runtime { get; }
}