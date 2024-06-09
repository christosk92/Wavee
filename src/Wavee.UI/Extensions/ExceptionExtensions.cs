
using System;

namespace Wavee.UI.Extensions;

public static class ExceptionExtensions
{
    public static string ToTypeMessageString(this Exception ex)
    {
        var trimmed = ex.Message.Correct();

        if (trimmed.Length == 0)
        {
            return ex.GetType().Name;
        }
        else
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }
}