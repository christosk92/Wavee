using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Wavee.Core.Infrastructure.Traits;

namespace Wavee.Core.Infrastructure.Sys;

/// <summary>
/// File IO 
/// </summary>
public static class Log<RT>
    where RT : struct, HasLog<RT>
{
    public static Eff<RT, Unit> logInfo(string message, [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) =>
        from fs in default(RT).LogEff.Map(e => e.Log(LogLevel.Information, message, callerFilePath: callerFilePath,
            callerMemberName: callerMemberName, callerLineNumber: callerLineNumber))
        select fs;
}