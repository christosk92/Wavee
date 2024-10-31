using System.Diagnostics;
using Wavee.ViewModels.Microservices;

namespace Wavee.ViewModels.Helpers;

/// <summary>
/// Helper methods for Application lifetime related functions.
/// </summary>
public static class AppLifetimeHelper
{
    /// <summary>
    /// Attempts to start a new instance of the app with optional program arguments
    /// </summary>
    /// <remarks>
    /// This method is only functional on the published builds
    /// and not on debugging runs.
    /// </remarks>
    /// <param name="args">The program arguments to pass to the new instance.</param>
    public static void StartAppWithArgs(string args = "")
    {
        var path = Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException($"Invalid path: '{path}'");
        }

        var startInfo = ProcessStartInfoFactory.Make(path, args);
        using var p = Process.Start(startInfo);
    }

}