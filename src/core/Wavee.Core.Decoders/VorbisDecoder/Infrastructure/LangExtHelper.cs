using LanguageExt.Common;

namespace Wavee.Core.Decoders.VorbisDecoder.Infrastructure;

internal static class LangExtHelper
{
    public static Exception Error<T>(this Result<T> result)
    {
        return result.Match(
            Succ: _ => throw new InvalidOperationException("Unexpected success"),
            Fail: e => e
        );
    }
    
    public static T Success<T>(this Result<T> result)
    {
        return result.Match(
            Succ: v => v,
            Fail: e => throw new InvalidOperationException("Unexpected failure")
        );
    }
}