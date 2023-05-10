using System.Runtime.CompilerServices;
using LanguageExt.Attributes;
using Microsoft.Extensions.Logging;

namespace Wavee.Infrastructure.Traits;

public interface LogIO
{
    Unit Log(
        LogLevel level, string message,
        int additionalEntrySeparators = 0,
        bool additionalEntrySeparatorsLogFileOnlyMode = true, [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1);
}


/// <summary>
/// Type-class giving a struct the trait of supporting File IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasLog<RT>
    where RT : struct, HasLog<RT>
{
    /// <summary>
    /// Access the logging synchronous effect environment
    /// </summary>
    /// <returns>Logging synchronous effect environment</returns>
    Eff<RT, LogIO> LogEff { get; }
}