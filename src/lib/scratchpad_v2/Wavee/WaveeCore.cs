using System.Runtime.CompilerServices;
using Wavee.Infrastructure.Live;

[assembly: InternalsVisibleTo("Wavee.Spotify")]

namespace Wavee;

internal static class WaveeCore
{
    static WaveeCore()
    {
        Runtime = WaveeRuntime.New();
    }

    public static WaveeRuntime Runtime { get; }
}