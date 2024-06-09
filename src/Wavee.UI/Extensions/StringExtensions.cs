using System.Diagnostics.CodeAnalysis;

namespace Wavee.UI.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Corrects the string:
    /// If the string is null, it'll be empty.
    /// Trims the string.
    /// </summary>
    [return: NotNull]
    public static string Correct(this string? str)
    {
        return string.IsNullOrWhiteSpace(str)
            ? ""
            : str.Trim();
    }
}