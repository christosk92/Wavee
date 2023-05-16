using System.Runtime.CompilerServices;
using LanguageExt;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Infrastructure.Sys;
public static class Enc<RT>
    where RT : struct, HasEncoding<RT>
{
    /// <summary>
    /// Encoding
    /// </summary>
    /// <typeparam name="RT">Runtime environment</typeparam>
    /// <returns>Encoding</returns>
    public static Eff<RT, System.Text.Encoding> encoding
    {
        [MethodImpl(AffOpt.mops)]
        get => Eff<RT, System.Text.Encoding>(static rt => rt.Encoding);
    }
}
