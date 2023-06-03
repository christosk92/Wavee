using LanguageExt;
using System.Runtime.CompilerServices;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Infrastructure.Sys;

public static class Local<RT>
    where RT : struct, HasLocalPath<RT>
{
    /// <summary>
    /// Encoding
    /// </summary>
    /// <typeparam name="RT">Runtime environment</typeparam>
    /// <returns>Encoding</returns>
    public static Eff<RT, string> localDir
    {
        [MethodImpl(AffOpt.mops)]
        get => Eff<RT, string>(static rt => rt.Path);
    }
}
